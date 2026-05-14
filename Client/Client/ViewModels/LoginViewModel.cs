using Avalonia.Platform;
using Client.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
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

        public ReactiveCommand<Unit, Unit> LoginCommand { get; }
        public ReactiveCommand<Unit, Unit> GoToRegisterCommand { get; }

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
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Введите логин и пароль";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            var success = await _authService.LoginAsync(Email, Password);

            IsLoading = false;

            if (success)
            {
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

        private async Task GoToRegisterAsync()
        {
            await _navigationService.NavigateToRegisterAsync();
        }
    }
}

