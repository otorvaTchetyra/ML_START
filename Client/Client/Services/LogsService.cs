using Client.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Services;

public class LogsService
{
    private readonly JournalService _journalService;

    public LogsService(JournalService journalService)
    {
        _journalService = journalService;
    }

    public async Task<List<AppLog>?> GetLogsAsync()
    {
        var entries = await _journalService.GetEntriesAsync();
        return entries.Select(entry => new AppLog
        {
            Id = entry.Id,
            Timestamp = entry.Timestamp,
            Level = entry.Level,
            Category = entry.EventType?.Category ?? string.Empty,
            Source = entry.Source,
            Action = entry.Action,
            Message = entry.Message,
            DetailsJson = entry.DetailsJson,
            IsResolved = entry.IsResolved,
            Comment = entry.Comment
        }).ToList();
    }
}
