using Client.Models;
using Client.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace Client.ViewModels;

public class StatisticsViewModel : ViewModelBase, IRoutableViewModel
{
    public string? UrlPathSegment => "statistics";
    public IScreen HostScreen { get; }

    private readonly EventsService _eventsService;

    private DateTime _dateFrom;
    private DateTime _dateTo;
    private string _selectedPeriod = "День";
    private bool _isLoading;
    private string _statusText = string.Empty;

    // Коллекции для графиков
    private ObservableCollection<FeedingEvent> _events = new();
    private ISeries[] _intensitySeries;
    private ISeries[] _granuleSeries;

    // Статистика
    private double _averageIntensity;
    private int _totalGranules;
    private int _totalEvents;
    private double _maxIntensity;
    private bool IsInitialized;

    public DateTime DateFrom
    {
        get => _dateFrom;
        set => this.RaiseAndSetIfChanged(ref _dateFrom, value);
    }

    public DateTime DateTo
    {
        get => _dateTo;
        set => this.RaiseAndSetIfChanged(ref _dateTo, value);
    }

    public string SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPeriod, value);
            UpdateDateRangeByPeriod();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public double AverageIntensity
    {
        get => _averageIntensity;
        set => this.RaiseAndSetIfChanged(ref _averageIntensity, value);
    }

    public int TotalGranules
    {
        get => _totalGranules;
        set => this.RaiseAndSetIfChanged(ref _totalGranules, value);
    }

    public int TotalEvents
    {
        get => _totalEvents;
        set => this.RaiseAndSetIfChanged(ref _totalEvents, value);
    }

    public double MaxIntensity
    {
        get => _maxIntensity;
        set => this.RaiseAndSetIfChanged(ref _maxIntensity, value);
    }

    // График интенсивности
    public ISeries[] IntensitySeries
    {
        get => _intensitySeries;
        set => this.RaiseAndSetIfChanged(ref _intensitySeries, value);
    }

    // График количества гранул
    public ISeries[] GranuleSeries
    {
        get => _granuleSeries;
        set => this.RaiseAndSetIfChanged(ref _granuleSeries, value);
    }

    public List<string> Periods { get; } = new() { "День", "Неделя", "Месяц", "Произвольный" };

    public ReactiveCommand<Unit, Unit> LoadStatisticsCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<Unit, Unit> GoBackCommand { get; }

    public StatisticsViewModel(
        IScreen screen,
        EventsService eventsService,
        NavigationService navigationService)
    {
        HostScreen = screen;
        _eventsService = eventsService;

        // По умолчанию - последние 7 дней
        DateTo = DateTime.Now;
        DateFrom = DateTime.Now.AddDays(-7);

        LoadStatisticsCommand = ReactiveCommand.CreateFromTask(LoadStatisticsAsync);
        RefreshCommand = LoadStatisticsCommand;
        GoBackCommand = ReactiveCommand.CreateFromTask(navigationService.NavigateToMainAsync);

        // Пустые графики по умолчанию
        IntensitySeries = Array.Empty<ISeries>();
        GranuleSeries = Array.Empty<ISeries>();
    }
    public async Task InitializeAsync()
    {
        if (!IsInitialized)
        {
            await LoadStatisticsAsync();
            IsInitialized = true;
        }
    }
    private void UpdateDateRangeByPeriod()
    {
        DateTo = DateTime.Now;

        switch (SelectedPeriod)
        {
            case "День":
                DateFrom = DateTime.Now.AddDays(-1);
                break;
            case "Неделя":
                DateFrom = DateTime.Now.AddDays(-7);
                break;
            case "Месяц":
                DateFrom = DateTime.Now.AddMonths(-1);
                break;
            case "Произвольный":
                // Оставляем текущие значения
                break;
        }
    }

    private async Task LoadStatisticsAsync()
    {
        IsLoading = true;
        StatusText = "Загрузка статистики...";

        try
        {
            var events = await _eventsService.GetEventsAsync();
            _events.Clear();
            foreach (var e in events)
                _events.Add(e);

            BuildCharts();
            CalculateStats();

            StatusText = $"Загружено событий: {_events.Count}";
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildCharts()
    {
        if (_events.Count == 0)
        {
            IntensitySeries = Array.Empty<ISeries>();
            GranuleSeries = Array.Empty<ISeries>();
            return;
        }

        // Сортируем по времени
        var sortedEvents = _events.OrderBy(e => e.Timestamp).ToList();

        // Данные для графика интенсивности
        var intensityPoints = sortedEvents
            .Select(e => new DateTimePoint(e.Timestamp, e.IntensityPerSec))
            .ToArray();

        // Данные для графика количества гранул
        var granulePoints = sortedEvents
            .Select(e => new DateTimePoint(e.Timestamp, e.GranuleCount))
            .ToArray();

        IntensitySeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Values = intensityPoints,
                Name = "Интенсивность (гранул/сек)",
                Stroke = new SolidColorPaint(SKColor.FromHsl(100, 100, 255)),
                Fill = null,
                GeometrySize = 8,
                LineSmoothness = 0.5
            }
        };

        GranuleSeries = new ISeries[]
        {
            new ColumnSeries<DateTimePoint>
            {
                Values = granulePoints,
                Name = "Количество гранул",
                Fill = new SolidColorPaint(SKColor.FromHsl(54, 162, 232))
            }
        };
    }

    private void CalculateStats()
    {
        if (_events.Count == 0)
        {
            AverageIntensity = 0;
            TotalGranules = 0;
            TotalEvents = 0;
            MaxIntensity = 0;
            return;
        }

        TotalEvents = _events.Count;
        TotalGranules = _events.Sum(e => e.GranuleCount);
        AverageIntensity = _events.Average(e => e.IntensityPerSec);
        MaxIntensity = _events.Max(e => e.IntensityPerSec);
    }
}