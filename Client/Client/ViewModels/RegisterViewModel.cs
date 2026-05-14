using Client.Services;
using ReactiveUI;
using System;
using System.Reactive;
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

        private Task LoadData() => Task.CompletedTask;

        private readonly NavigationService _navigationService;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _errorMessage = string.Empty;
        private string _infoMessage = string.Empty;
        private bool _isLoading;

        public string Username
        {
            get => _username;
            set
            {
                this.RaiseAndSetIfChanged(ref _username, value);
                InfoMessage = string.Empty;
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                this.RaiseAndSetIfChanged(ref _password, value);
                InfoMessage = string.Empty;
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                this.RaiseAndSetIfChanged(ref _confirmPassword, value);
                InfoMessage = string.Empty;
            }
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

        public string InfoMessage
        {
            get => _infoMessage;
            set
            {
                this.RaiseAndSetIfChanged(ref _infoMessage, value);
                this.RaisePropertyChanged(nameof(HasInfo));
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public ReactiveCommand<Unit, Unit> RegisterCommand { get; }
        public ReactiveCommand<Unit, Unit> GoToLoginCommand { get; }

        public RegisterViewModel(IScreen screen, NavigationService navigationService)
        {
            _navigationService = navigationService;
            HostScreen = screen;

            RegisterCommand = ReactiveCommand.CreateFromTask(RegisterAsync);
            GoToLoginCommand = ReactiveCommand.CreateFromTask(GoToLoginAsync);
        }

        private async Task RegisterAsync()
        {
            ErrorMessage = string.Empty;
            InfoMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Введите логин";
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
            try
            {
                await Task.Yield();
                InfoMessage =
                    "Самостоятельная регистрация в приложении отключена. " +
                    "Новые учётные записи создаёт администратор: войдите как admin → «Админ-панель» → «Учётные записи».";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GoToLoginAsync()
        {
            await _navigationService.NavigateToLoginAsync();
        }
    }
}
