using Client.Data;
using Client.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Client.Services;

public class JournalService
{
    private readonly JournalDbContext _dbContext;

    public JournalService(JournalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<JournalEntry>> GetEntriesAsync(
        int limit = 200,
        string? level = null,
        string? category = null,
        string? source = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        bool? resolved = null)
    {
        try
        {
            await JournalDatabaseInitializer.EnsureReadyAsync(_dbContext);

            var query = _dbContext.Entries
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

            return await query
                .OrderByDescending(x => x.Timestamp)
                .Take(limit)
                .ToListAsync();
        }
        catch
        {
            return new List<JournalEntry>();
        }
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
        try
        {
            await JournalDatabaseInitializer.EnsureReadyAsync(_dbContext);

            var eventType = await _dbContext.EventTypes
                .FirstOrDefaultAsync(x => x.Code == eventCode)
                ?? await _dbContext.EventTypes.FirstAsync(x => x.Code == "custom");

            var entry = new JournalEntry
            {
                Timestamp = DateTime.UtcNow,
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
                ResolvedAt = isResolved ? DateTime.UtcNow : null
            };

            _dbContext.Entries.Add(entry);
            await _dbContext.SaveChangesAsync();
            entry.EventType = eventType;
            return entry;
        }
        catch
        {
            return new JournalEntry
            {
                Timestamp = DateTime.UtcNow,
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
                ResolvedAt = isResolved ? DateTime.UtcNow : null
            };
        }
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
            await JournalDatabaseInitializer.EnsureReadyAsync(_dbContext);

            var entry = await _dbContext.Entries.FirstOrDefaultAsync(x => x.Id == entryId);
            if (entry == null)
                return;

            entry.Comment = comment;
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
        }
    }

    public async Task MarkResolvedAsync(long entryId, bool isResolved = true)
    {
        try
        {
            await JournalDatabaseInitializer.EnsureReadyAsync(_dbContext);

            var entry = await _dbContext.Entries.FirstOrDefaultAsync(x => x.Id == entryId);
            if (entry == null)
                return;

            entry.IsResolved = isResolved;
            entry.ResolvedAt = isResolved ? DateTime.UtcNow : null;
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
        }
    }
}
