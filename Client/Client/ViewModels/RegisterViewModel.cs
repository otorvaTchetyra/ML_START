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
    public class RegisterViewModel : ViewModelBase, IRoutableViewModel
    {
        private bool IsInitialized;

        public string? UrlPathSegment => "register";
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

        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
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

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => this.RaiseAndSetIfChanged(ref _confirmPassword, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public ReactiveCommand<Unit, Unit> RegisterCommand { get; }
        public ReactiveCommand<Unit, Unit> GoToLoginCommand { get; }

        public RegisterViewModel(IScreen screen, AuthService authService, NavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
            HostScreen = screen;

            RegisterCommand = ReactiveCommand.CreateFromTask(RegisterAsync);
            GoToLoginCommand = ReactiveCommand.CreateFromTask(GoToLoginAsync);
        }

        private async Task RegisterAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Введите email";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Введите пароль";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Пароли не совпадают";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            /*var result = await _authService.RegisterAsync(Email, Password);

            IsLoading = false;

            if (result.Success)
            {
                await _navigationService.NavigateToLoginAsync();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Ошибка регистрации";
            }*/
        }

        private async Task GoToLoginAsync()
        {
            await _navigationService.NavigateToLoginAsync();
        }
    }

}

