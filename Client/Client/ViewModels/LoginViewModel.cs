using Avalonia.Threading;
using Client.Services;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace Client.ViewModels
{
    public class LoginViewModel: ViewModelBase, IRoutableViewModel
    {
        private bool IsInitialized;

        public string? UrlPathSegment => "login";
        public IScreen HostScreen { get; }

        public async Task InitializeAsync()
        {
            if (!IsInitialized)
            {
                await LoadData();
                IsInitialized = true;
            }
        }

        private async Task LoadData()
        {
        }
        private readonly AuthService _authService;
        private readonly NavigationService _navigationService;
        private readonly JournalService _journalService;

        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isLoading;
        private bool _isPasswordVisible;
        private int _failedAttempts;
        private int _countdownSeconds;
        private System.Threading.Timer? _countdownTimer;

        private const int MaxAttempts = 5;

        public string Email
        {
            get => _email;
            set => this.RaiseAndSetIfChanged(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                this.RaiseAndSetIfChanged(ref _errorMessage, value);
                this.RaisePropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set => this.RaiseAndSetIfChanged(ref _isPasswordVisible, value);
        }

        public bool IsRateLimited => _countdownSeconds > 0;
        public bool HasAttemptsWarning => _failedAttempts > 0 && _countdownSeconds == 0;
        public string AttemptsText => $"Осталось попыток: {MaxAttempts - _failedAttempts}";
        public string CountdownText => $"Слишком много попыток. Повторите через {_countdownSeconds} с";

        public ReactiveCommand<Unit, Unit> LoginCommand { get; }
        public ReactiveCommand<Unit, Unit> GoToRegisterCommand { get; }
        public ReactiveCommand<Unit, Unit> TogglePasswordCommand { get; }

        public LoginViewModel(
            IScreen screen,
            AuthService authService,
            NavigationService navigationService,
            JournalService journalService)
        {
            _authService = authService;
            _navigationService = navigationService;
            _journalService = journalService;
            HostScreen = screen;

            LoginCommand = ReactiveCommand.CreateFromTask(LoginAsync);
            GoToRegisterCommand = ReactiveCommand.CreateFromTask(GoToRegisterAsync);
            TogglePasswordCommand = ReactiveCommand.Create(() => { IsPasswordVisible = !IsPasswordVisible; });
        }

        private async Task LoginAsync()
        {
            if (IsRateLimited) return;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Введите логин и пароль";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            var result = await _authService.LoginAsync(Email, Password);

            IsLoading = false;

            if (result.IsRateLimited)
            {
                StartCountdown(result.RetryAfterSeconds);
                return;
            }

            if (result.Success)
            {
                _failedAttempts = 0;
                this.RaisePropertyChanged(nameof(HasAttemptsWarning));
                _ = _journalService.RecordAsync(
                    eventCode: "login_success",
                    message: $"Пользователь {Email} вошёл в приложение",
                    source: "auth",
                    action: "login",
                    level: "info",
                    usernameSnapshot: Email);
                if (_authService.IsAdmin)
                    await _navigationService.NavigateToAdminHomeAsync();
                else
                    await _navigationService.NavigateToMainAsync();
            }
            else
            {
                _failedAttempts++;
                this.RaisePropertyChanged(nameof(AttemptsText));
                this.RaisePropertyChanged(nameof(HasAttemptsWarning));
                _ = _journalService.RecordAsync(
                    eventCode: "login_failed",
                    message: $"Неудачная попытка входа для пользователя {Email}",
                    source: "auth",
                    action: "login",
                    level: "warning",
                    usernameSnapshot: Email);
                ErrorMessage = "Неверный логин или пароль";
            }
        }

        private void StartCountdown(int seconds)
        {
            _countdownSeconds = seconds;
            this.RaisePropertyChanged(nameof(IsRateLimited));
            this.RaisePropertyChanged(nameof(CountdownText));
            this.RaisePropertyChanged(nameof(HasAttemptsWarning));

            _countdownTimer?.Dispose();
            _countdownTimer = new System.Threading.Timer(_ =>
            {
                if (_countdownSeconds > 0)
                    _countdownSeconds--;

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    this.RaisePropertyChanged(nameof(CountdownText));
                    this.RaisePropertyChanged(nameof(IsRateLimited));
                    this.RaisePropertyChanged(nameof(HasAttemptsWarning));

                    if (_countdownSeconds == 0)
                    {
                        _failedAttempts = 0;
                        this.RaisePropertyChanged(nameof(AttemptsText));
                        _countdownTimer?.Dispose();
                        _countdownTimer = null;
                    }
                });
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private async Task GoToRegisterAsync()
        {
            await _navigationService.NavigateToRegisterAsync();
        }
    }
}

