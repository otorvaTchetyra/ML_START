using System.Collections.Generic;

namespace Client.Models;

public class JournalEventType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DefaultSeverity { get; set; } = "info";
    public bool RequiresComment { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<JournalEntry> Entries { get; set; } = new List<JournalEntry>();
}
