using Avalonia.Media;
using Client.Models;
using Client.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

namespace Client.ViewModels;

public class AdminJournalViewModel : ViewModelBase, IRoutableViewModel
{
    public const string AllUsersLabel = "Все пользователи";

    private bool _isInitialized;

    public string? UrlPathSegment => "admin-journal";
    public IScreen HostScreen { get; }

    private readonly JournalService _journalService;
    private readonly UsersService _usersService;
    private readonly NavigationService _navigationService;

    private ObservableCollection<JournalEntry> _entries = new();
    private ObservableCollection<string> _filterOptions = new() { AllUsersLabel };
    private string _selectedFilter = AllUsersLabel;
    private string _statusMessage = string.Empty;
    private IBrush _statusBrush = Brushes.Transparent;
    private bool _suppressFilterLoad;

    public ObservableCollection<JournalEntry> Entries
    {
        get => _entries;
        set => this.RaiseAndSetIfChanged(ref _entries, value);
    }

    public ObservableCollection<string> FilterOptions
    {
        get => _filterOptions;
        set => this.RaiseAndSetIfChanged(ref _filterOptions, value);
    }

    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (EqualityComparer<string>.Default.Equals(_selectedFilter, value))
                return;
            this.RaiseAndSetIfChanged(ref _selectedFilter, value);
            if (!_suppressFilterLoad)
                _ = LoadEntriesAsync();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public IBrush StatusBrush
    {
        get => _statusBrush;
        set => this.RaiseAndSetIfChanged(ref _statusBrush, value);
    }

    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    public AdminJournalViewModel(
        IScreen screen,
        JournalService journalService,
        UsersService usersService,
        NavigationService navigationService)
    {
        HostScreen = screen;
        _journalService = journalService;
        _usersService = usersService;
        _navigationService = navigationService;

        BackCommand = ReactiveCommand.CreateFromTask(() => _navigationService.NavigateToAdminHomeAsync());
        RefreshCommand = ReactiveCommand.CreateFromTask(LoadAllAsync);
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;
        _isInitialized = true;
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        try
        {
            await RebuildFilterOptionsAsync();
            await LoadEntriesAsync();
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            StatusBrush = Brushes.OrangeRed;
        }
    }

    private async Task RebuildFilterOptionsAsync()
    {
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var users = await _usersService.GetUsersAsync();
            if (users != null)
            {
                foreach (var u in users)
                {
                    if (!string.IsNullOrWhiteSpace(u.Username))
                        names.Add(u.Username);
                }
            }
        }
        catch
        {
        }

        var fromJournal = await _journalService.GetDistinctJournalUsernamesAsync();
        foreach (var n in fromJournal)
            names.Add(n);

        var options = new ObservableCollection<string> { AllUsersLabel };
        foreach (var n in names)
            options.Add(n);

        _suppressFilterLoad = true;
        try
        {
            FilterOptions = options;
            if (!FilterOptions.Contains(SelectedFilter))
                SelectedFilter = AllUsersLabel;
        }
        finally
        {
            _suppressFilterLoad = false;
        }
    }

    private async Task LoadEntriesAsync()
    {
        try
        {
            var filter = string.Equals(SelectedFilter, AllUsersLabel, StringComparison.Ordinal)
                ? null
                : SelectedFilter;
            var list = await _journalService.GetEntriesAsync(limit: 400, usernameSnapshot: filter);
            Entries = new ObservableCollection<JournalEntry>(list);
        }
        catch
        {
            Entries = new ObservableCollection<JournalEntry>();
        }
    }
}
