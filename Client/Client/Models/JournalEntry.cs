using System;

namespace Client.Models;

public class JournalEntry
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = "info";
    public int EventTypeId { get; set; }
    public JournalEventType? EventType { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
    public int? UserId { get; set; }
    public string? UsernameSnapshot { get; set; }
    public string? Screen { get; set; }
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Comment { get; set; }

    public bool HasComment => !string.IsNullOrWhiteSpace(Comment);
    public string Category => EventType?.Category ?? string.Empty;
}
