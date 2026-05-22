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

    private readonly AuthService _authService;
    private readonly EventsService _eventsService;

    private string _selectedPeriod = "День";
    private bool _isLoading;
    private string _statusText = string.Empty;

    private ObservableCollection<FeedingEvent> _events = new();
    private ISeries[] _intensitySeries;
    private ISeries[] _granuleSeries;

    private double _averageIntensity;
    private int _totalGranules;
    private int _totalEvents;
    private double _maxIntensity;
    private bool IsInitialized;



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

    public ISeries[] IntensitySeries
    {
        get => _intensitySeries;
        set => this.RaiseAndSetIfChanged(ref _intensitySeries, value);
    }

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
        AuthService authService,
        EventsService eventsService,
        NavigationService navigationService)
    {
        HostScreen = screen;
        _eventsService = eventsService;
        _authService = authService;


        LoadStatisticsCommand = ReactiveCommand.CreateFromTask(LoadStatisticsAsync);
        RefreshCommand = LoadStatisticsCommand;
        GoBackCommand = ReactiveCommand.CreateFromTask(navigationService.NavigateToMainAsync);

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

        var sortedEvents = _events.OrderBy(e => e.Timestamp).ToList();
        var dates = sortedEvents.Select(e => e.Timestamp).ToList();
        var intensityValues = sortedEvents.Select(e => (double)e.IntensityPerSec).ToArray();
        var granuleValues = sortedEvents.Select(e => (double)e.GranuleCount).ToArray();

        IntensitySeries = new ISeries[]
        {
        new ColumnSeries<double>
        {
            Values = intensityValues,
            Name = "Интенсивность (гранул/сек)",
            Fill = new SolidColorPaint(SKColor.Parse("#FF6B6B")),
            XToolTipLabelFormatter = (point) =>
            {
                var index = point.Index;
                var date = index < dates.Count ? dates[index].ToString("HH:mm:ss") : index.ToString();
                var value = point.Model; 
                return $"Событие {index + 1}: {date}";
            }
        }
        };

        GranuleSeries = new ISeries[]
        {
        new ColumnSeries<double>
        {
            Values = granuleValues,
            Name = "Количество гранул",
            Fill = new SolidColorPaint(SKColor.Parse("#4CAF50")),
            XToolTipLabelFormatter = (point) =>
            {
                var index = point.Index; 
                var date = index < dates.Count ? dates[index].ToString("HH:mm:ss") : index.ToString();
                var value = point.Model; 
                return $"Событие {index + 1}: {date}";
            }
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