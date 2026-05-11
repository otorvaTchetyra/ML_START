using System;

namespace Client.Models;

public class AppLog
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
    public bool IsResolved { get; set; }
    public string? Comment { get; set; }

    public bool HasComment => !string.IsNullOrWhiteSpace(Comment);
}
