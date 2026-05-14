using Client.Services;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;

namespace Client.ViewModels;

public class AdminHomeViewModel : ViewModelBase, IRoutableViewModel
{
    private bool _isInitialized;

    public string? UrlPathSegment => "admin-home";
    public IScreen HostScreen { get; }

    private readonly AuthService _authService;
    private readonly NavigationService _navigationService;
    private readonly JournalService _journalService;

    public string WelcomeText =>
        string.IsNullOrWhiteSpace(_authService.CurrentUser?.Username)
            ? "Панель администратора"
            : $"Здравствуйте, {_authService.CurrentUser!.Username}";

    public ReactiveCommand<Unit, Unit> GoToUsersCommand { get; }
    public ReactiveCommand<Unit, Unit> GoToJournalCommand { get; }
    public ReactiveCommand<Unit, Unit> GoToSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> GoToVideoCommand { get; }
    public ReactiveCommand<Unit, Unit> GoToStreamCommand { get; }
    public ReactiveCommand<Unit, Unit> LogoutCommand { get; }

    public AdminHomeViewModel(
        IScreen screen,
        AuthService authService,
        NavigationService navigationService,
        JournalService journalService)
    {
        HostScreen = screen;
        _authService = authService;
        _navigationService = navigationService;
        _journalService = journalService;

        GoToUsersCommand = ReactiveCommand.CreateFromTask(() => _navigationService.NavigateToAdminUsersAsync());
        GoToJournalCommand = ReactiveCommand.CreateFromTask(() => _navigationService.NavigateToAdminJournalAsync());
        GoToSettingsCommand = ReactiveCommand.CreateFromTask(() => _navigationService.NavigateToSettingsAsync());
        GoToVideoCommand = ReactiveCommand.CreateFromTask(() => _navigationService.NavigateToMainAsync());
        GoToStreamCommand = ReactiveCommand.CreateFromTask(() => _navigationService.NavigateToStreamAsync());
        LogoutCommand = ReactiveCommand.CreateFromTask(LogoutAsync);
    }

    public Task InitializeAsync()
    {
        if (_isInitialized)
            return Task.CompletedTask;
        _isInitialized = true;
        this.RaisePropertyChanged(nameof(WelcomeText));
        return Task.CompletedTask;
    }

    private async Task LogoutAsync()
    {
        await _journalService.RecordAsync(
            eventCode: "logout",
            message: "Пользователь вышел из приложения",
            source: "auth",
            action: "logout",
            level: "info",
            usernameSnapshot: _authService.CurrentUser?.Username);
        _authService.Logout();
        await _navigationService.NavigateToLoginAsync();
    }
}
