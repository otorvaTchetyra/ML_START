using Client.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Data;

public static class JournalDatabaseInitializer
{
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private static bool _isInitialized;

    public static async Task EnsureReadyAsync(JournalDbContext dbContext)
    {
        if (_isInitialized)
            return;

        await InitLock.WaitAsync();
        try
        {
            if (_isInitialized)
                return;

            await dbContext.Database.EnsureCreatedAsync();

            if (!await dbContext.EventTypes.AnyAsync())
            {
                dbContext.EventTypes.AddRange(DefaultEventTypes());
                await dbContext.SaveChangesAsync();
            }

            _isInitialized = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    private static IEnumerable<JournalEventType> DefaultEventTypes()
    {
        return new[]
        {
            new JournalEventType { Code = "app_start", Name = "Запуск приложения", Category = "system", DefaultSeverity = "info", SortOrder = 10 },
            new JournalEventType { Code = "login_success", Name = "Успешный вход", Category = "auth", DefaultSeverity = "info", SortOrder = 20 },
            new JournalEventType { Code = "login_failed", Name = "Неудачный вход", Category = "auth", DefaultSeverity = "warning", SortOrder = 21 },
            new JournalEventType { Code = "logout", Name = "Выход", Category = "auth", DefaultSeverity = "info", SortOrder = 22 },
            new JournalEventType { Code = "settings_saved", Name = "Сохранение настроек", Category = "settings", DefaultSeverity = "info", SortOrder = 30 },
            new JournalEventType { Code = "connection_test_success", Name = "Проверка связи успешна", Category = "settings", DefaultSeverity = "info", SortOrder = 31 },
            new JournalEventType { Code = "connection_test_failed", Name = "Проверка связи неудачна", Category = "settings", DefaultSeverity = "warning", SortOrder = 32 },
            new JournalEventType { Code = "stream_started", Name = "Запуск потока", Category = "stream", DefaultSeverity = "info", SortOrder = 40 },
            new JournalEventType { Code = "stream_stopped", Name = "Остановка потока", Category = "stream", DefaultSeverity = "info", SortOrder = 41 },
            new JournalEventType { Code = "video_opened", Name = "Открытие видео", Category = "stream", DefaultSeverity = "info", SortOrder = 42 },
            new JournalEventType { Code = "detection_found", Name = "Обнаружение события", Category = "detection", DefaultSeverity = "info", SortOrder = 50 },
            new JournalEventType { Code = "threshold_exceeded", Name = "Превышен порог", Category = "detection", DefaultSeverity = "warning", SortOrder = 51, RequiresComment = true },
            new JournalEventType { Code = "out_of_schedule", Name = "Вне расписания", Category = "detection", DefaultSeverity = "warning", SortOrder = 52, RequiresComment = true },
            new JournalEventType { Code = "comment_added", Name = "Комментарий добавлен", Category = "detection", DefaultSeverity = "info", SortOrder = 53 },
            new JournalEventType { Code = "error", Name = "Ошибка", Category = "system", DefaultSeverity = "error", SortOrder = 90, RequiresComment = true },
            new JournalEventType { Code = "warning", Name = "Предупреждение", Category = "system", DefaultSeverity = "warning", SortOrder = 91 },
            new JournalEventType { Code = "custom", Name = "Пользовательское событие", Category = "custom", DefaultSeverity = "info", SortOrder = 999 },
        };
    }
}
