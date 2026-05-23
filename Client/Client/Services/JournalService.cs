using Client.Data;
using Client.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Client.Services;

public class JournalService
{
    private readonly IDbContextFactory<JournalDbContext> _dbContextFactory;
    private readonly IApiClient _apiClient;

    public JournalService(IDbContextFactory<JournalDbContext> dbContextFactory, IApiClient apiClient)
    {
        _dbContextFactory = dbContextFactory;
        _apiClient = apiClient;
    }

    private class ServerJournalEntry
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; }
        [JsonPropertyName("level")] public string Level { get; set; } = "";
        [JsonPropertyName("source")] public string Source { get; set; } = "";
        [JsonPropertyName("action")] public string Action { get; set; } = "";
        [JsonPropertyName("message")] public string Message { get; set; } = "";
        [JsonPropertyName("details_json")] public string? DetailsJson { get; set; }
        [JsonPropertyName("user_id")] public int? UserId { get; set; }
        [JsonPropertyName("username_snapshot")] public string? UsernameSnapshot { get; set; }
        [JsonPropertyName("screen")] public string? Screen { get; set; }
        [JsonPropertyName("entity_type")] public string? EntityType { get; set; }
        [JsonPropertyName("entity_id")] public long? EntityId { get; set; }
        [JsonPropertyName("is_resolved")] public bool IsResolved { get; set; }
        [JsonPropertyName("resolved_at")] public DateTime? ResolvedAt { get; set; }
        [JsonPropertyName("comment")] public string? Comment { get; set; }
        [JsonPropertyName("event_code")] public string? EventCode { get; set; }
    }

    public async Task<List<JournalEntry>> GetEntriesAsync(
        int limit = 200,
        string? level = null,
        string? category = null,
        string? source = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        bool? resolved = null,
        string? usernameSnapshot = null)
    {
        try
        {
            var url = $"/journal/entries?limit={limit}";
            if (!string.IsNullOrWhiteSpace(level)) url += $"&level={level}";
            if (!string.IsNullOrWhiteSpace(source)) url += $"&source={source}";

            var serverEntries = await _apiClient.GetAsync<List<ServerJournalEntry>>(url);
            if (serverEntries != null)
            {
                var result = serverEntries
                    .Where(e =>
                        (string.IsNullOrWhiteSpace(usernameSnapshot) || e.UsernameSnapshot == usernameSnapshot) &&
                        (!dateFrom.HasValue || e.Timestamp >= dateFrom.Value) &&
                        (!dateTo.HasValue || e.Timestamp <= dateTo.Value) &&
                        (!resolved.HasValue || e.IsResolved == resolved.Value))
                    .Select(e => new JournalEntry
                    {
                        Id = e.Id,
                        Timestamp = e.Timestamp,
                        Level = e.Level,
                        Source = e.Source,
                        Action = e.Action,
                        Message = e.Message,
                        DetailsJson = e.DetailsJson,
                        UserId = e.UserId,
                        UsernameSnapshot = e.UsernameSnapshot,
                        Screen = e.Screen,
                        EntityType = e.EntityType,
                        EntityId = e.EntityId,
                        IsResolved = e.IsResolved,
                        ResolvedAt = e.ResolvedAt,
                        Comment = e.Comment,
                    })
                    .ToList();
                return result;
            }
        }
        catch { }

        // fallback — локальная БД
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await JournalDatabaseInitializer.EnsureReadyAsync(dbContext);

        var query = dbContext.Entries
            .Include(x => x.EventType)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(x => x.Level == level);
        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(x => x.Source == source);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(x => x.EventType != null && x.EventType.Category == category);
        if (dateFrom.HasValue)
            query = query.Where(x => x.Timestamp >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(x => x.Timestamp <= dateTo.Value);
        if (resolved.HasValue)
            query = query.Where(x => x.IsResolved == resolved.Value);
        if (!string.IsNullOrWhiteSpace(usernameSnapshot))
            query = query.Where(x => x.UsernameSnapshot == usernameSnapshot);

        return await query
            .OrderByDescending(x => x.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctJournalUsernamesAsync()
    {
        try
        {
            var entries = await _apiClient.GetAsync<List<ServerJournalEntry>>("/journal/entries?limit=1000");
            if (entries != null)
                return entries
                    .Where(x => !string.IsNullOrWhiteSpace(x.UsernameSnapshot))
                    .Select(x => x.UsernameSnapshot!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();
        }
        catch { }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await JournalDatabaseInitializer.EnsureReadyAsync(dbContext);

        return await dbContext.Entries
            .AsNoTracking()
            .Where(x => x.UsernameSnapshot != null && x.UsernameSnapshot != string.Empty)
            .Select(x => x.UsernameSnapshot!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    public async Task<JournalEntry> RecordAsync(
        string eventCode,
        string message,
        string source = "client",
        string action = "event",
        string level = "info",
        string? detailsJson = null,
        int? userId = null,
        string? usernameSnapshot = null,
        string? screen = null,
        string? entityType = null,
        long? entityId = null,
        string? comment = null,
        bool isResolved = false)
    {
        var now = DateTime.UtcNow;
        JournalEntry entry;

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            await JournalDatabaseInitializer.EnsureReadyAsync(dbContext);

            var eventType = await dbContext.EventTypes
                .FirstOrDefaultAsync(x => x.Code == eventCode)
                ?? await dbContext.EventTypes.FirstAsync(x => x.Code == "custom");

            entry = new JournalEntry
            {
                Timestamp = now,
                Level = level,
                EventTypeId = eventType.Id,
                Source = source,
                Action = action,
                Message = message,
                DetailsJson = detailsJson,
                UserId = userId,
                UsernameSnapshot = usernameSnapshot,
                Screen = screen,
                EntityType = entityType,
                EntityId = entityId,
                Comment = comment,
                IsResolved = isResolved,
                ResolvedAt = isResolved ? now : null
            };

            dbContext.Entries.Add(entry);
            await dbContext.SaveChangesAsync();
            entry.EventType = eventType;
        }
        catch
        {
            entry = new JournalEntry
            {
                Timestamp = now,
                Level = level,
                Source = source,
                Action = action,
                Message = message,
                DetailsJson = detailsJson,
                UserId = userId,
                UsernameSnapshot = usernameSnapshot,
                Screen = screen,
                EntityType = entityType,
                EntityId = entityId,
                Comment = comment,
                IsResolved = isResolved,
                ResolvedAt = isResolved ? now : null
            };
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _apiClient.PostAsync<object>("/journal/entries", new
                {
                    timestamp = entry.Timestamp,
                    level = entry.Level,
                    event_code = eventCode,
                    source = entry.Source,
                    action = entry.Action,
                    message = entry.Message,
                    details_json = entry.DetailsJson,
                    user_id = entry.UserId,
                    username_snapshot = entry.UsernameSnapshot,
                    screen = entry.Screen,
                    entity_type = entry.EntityType,
                    entity_id = entry.EntityId,
                    is_resolved = entry.IsResolved,
                    comment = entry.Comment,
                });
            }
            catch { }
        });

        return entry;
    }

    public Task<JournalEntry> RecordAsync(
        string eventCode,
        string message,
        object details,
        string source = "client",
        string action = "event",
        string level = "info",
        int? userId = null,
        string? usernameSnapshot = null,
        string? screen = null,
        string? entityType = null,
        long? entityId = null,
        string? comment = null,
        bool isResolved = false)
    {
        var detailsJson = JsonSerializer.Serialize(details);
        return RecordAsync(
            eventCode,
            message,
            source,
            action,
            level,
            detailsJson,
            userId,
            usernameSnapshot,
            screen,
            entityType,
            entityId,
            comment,
            isResolved);
    }

    public async Task AddCommentAsync(long entryId, string comment)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            await JournalDatabaseInitializer.EnsureReadyAsync(dbContext);

            var entry = await dbContext.Entries.FirstOrDefaultAsync(x => x.Id == entryId);
            if (entry == null)
                return;

            entry.Comment = comment;
            await dbContext.SaveChangesAsync();
        }
        catch
        {
        }
    }

    public async Task MarkResolvedAsync(long entryId, bool isResolved = true)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            await JournalDatabaseInitializer.EnsureReadyAsync(dbContext);

            var entry = await dbContext.Entries.FirstOrDefaultAsync(x => x.Id == entryId);
            if (entry == null)
                return;

            entry.IsResolved = isResolved;
            entry.ResolvedAt = isResolved ? DateTime.UtcNow : null;
            await dbContext.SaveChangesAsync();
        }
        catch
        {
        }
    }
}
