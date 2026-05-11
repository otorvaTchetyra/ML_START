using Avalonia.Media;
using Client.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Reactive;
using System.Threading.Tasks;

namespace Client.ViewModels
{
    public class SettingsViewModel : ViewModelBase, IRoutableViewModel
    {
        private readonly IConfigurationService _configurationService;
        private readonly IHealthService _healthService;
        private readonly NavigationService _navigationService;
        private readonly JournalService _journalService;
        private readonly IApiClient _apiClient;

        private bool IsInitialized;

        public string? UrlPathSegment => "settings";
        public IScreen HostScreen { get; }

        private string _urlServerPath = string.Empty;
        private string _connectionStatus = string.Empty;
        private bool _isTestingConnection;
        private float _confidence = 0.5f;
        private float _iou = 0.45f;
        private string _selectedTheme = "Темная";
        private string _statusMessage = string.Empty;
        private IBrush _statusMessageColor = Brushes.Transparent;
        private bool _isStatusVisible;

        public string UrlServerPath
        {
            get => _urlServerPath;
            set => this.RaiseAndSetIfChanged(ref _urlServerPath, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
        }

        public bool IsTestingConnection
        {
            get => _isTestingConnection;
            set => this.RaiseAndSetIfChanged(ref _isTestingConnection, value);
        }

        public float Confidence
        {
            get => _confidence;
            set => this.RaiseAndSetIfChanged(ref _confidence, value);
        }

        public float Iou
        {
            get => _iou;
            set => this.RaiseAndSetIfChanged(ref _iou, value);
        }

        public ObservableCollection<string> ThemeOptions { get; } = new()
        {
            "Темная",
            "Светлая"
        };

        public string SelectedTheme
        {
            get => _selectedTheme;
            set => this.RaiseAndSetIfChanged(ref _selectedTheme, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public IBrush StatusMessageColor
        {
            get => _statusMessageColor;
            set => this.RaiseAndSetIfChanged(ref _statusMessageColor, value);
        }

        public bool IsStatusVisible
        {
            get => _isStatusVisible;
            set => this.RaiseAndSetIfChanged(ref _isStatusVisible, value);
        }

        public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> TestConnectionCommand { get; }
        public ReactiveCommand<Unit, Unit> GoToMainCommand { get; }
        public ReactiveCommand<Unit, Unit> GoToStreamCommand { get; }
        public ReactiveCommand<Unit, Unit> DropDBCommand { get; }

        public SettingsViewModel(
            IConfigurationService configurationService,
            IHealthService healthService,
            IScreen screen,
            IApiClient apiClient,
            NavigationService navigationService,
            JournalService journalService)
        {
            _configurationService = configurationService;
            _navigationService = navigationService;
            _healthService = healthService;
            _journalService = journalService;
            _apiClient = apiClient;
            HostScreen = screen;

            SaveSettingsCommand = ReactiveCommand.CreateFromTask(SaveSettingsAsync);
            TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnectionAsync);
            GoToMainCommand = ReactiveCommand.CreateFromTask(GoToMainAsync);
            GoToStreamCommand = ReactiveCommand.CreateFromTask(GoToStreamAsync);
            DropDBCommand = ReactiveCommand.CreateFromTask(DropDBAsync);
        }

        private async Task GoToMainAsync()
        {
            await _navigationService.NavigateToMainAsync();
        }

        private async Task GoToStreamAsync()
        {
            await _navigationService.NavigateToStreamAsync();
        }

        private async Task DropDBAsync()
        {
            await _healthService.DropDBAsync();
        }

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
            var config = _configurationService.GetConfig();
            UrlServerPath = _configurationService.GetApiUrl();

            try
            {
                Confidence = float.Parse(config["NeuralNetwork:Confidence"] ?? "0.5", CultureInfo.InvariantCulture);
                Iou = float.Parse(config["NeuralNetwork:Iou"] ?? "0.45", CultureInfo.InvariantCulture);
                SelectedTheme = string.Equals(_configurationService.GetTheme(), "Light", StringComparison.OrdinalIgnoreCase)
                    ? "Светлая"
                    : "Темная";
            }
            catch
            {
                Confidence = 0.5f;
                Iou = 0.45f;
                SelectedTheme = "Темная";
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                IsStatusVisible = true;
                StatusMessage = "Сохранение настроек...";
                StatusMessageColor = Brushes.Gray;

                _configurationService.SaveApiUrl(UrlServerPath);
                var theme = SelectedTheme == "Светлая" ? "Light" : "Dark";
                _configurationService.SaveSettings(Confidence, Iou, theme);
                global::Client.App.ApplyTheme(theme);
                await _apiClient.PatchAsync<object>(
                    "/settings",
                    new
                    {
                        model_confidence = Confidence,
                        model_iou = Iou
                    });

                await _journalService.RecordAsync(
                    eventCode: "settings_saved",
                    message: "Пользователь сохранил настройки приложения",
                    source: "settings",
                    action: "save",
                    level: "info",
                    detailsJson: $"{{\"apiUrl\":\"{UrlServerPath}\",\"confidence\":{Confidence.ToString(CultureInfo.InvariantCulture)},\"iou\":{Iou.ToString(CultureInfo.InvariantCulture)},\"theme\":\"{theme}\"}}");

                StatusMessage = "Настройки успешно сохранены";
                StatusMessageColor = Brushes.Green;

                await Task.Delay(2000);
                IsStatusVisible = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка сохранения: {ex.Message}";
                StatusMessageColor = Brushes.Red;
            }
        }

        private async Task TestConnectionAsync()
        {
            IsTestingConnection = true;
            ConnectionStatus = "Проверка...";

            try
            {
                using var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(UrlServerPath),
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var response = await httpClient.GetAsync("/health");
                response.EnsureSuccessStatusCode();

                await _journalService.RecordAsync(
                    eventCode: "connection_test_success",
                    message: "Проверка связи с сервером завершилась успешно",
                    source: "settings",
                    action: "test_connection",
                    level: "info");

                ConnectionStatus = "Сервер доступен";
                IsTestingConnection = false;
            }
            catch
            {
                await _journalService.RecordAsync(
                    eventCode: "connection_test_failed",
                    message: "Проверка связи с сервером завершилась ошибкой",
                    source: "settings",
                    action: "test_connection",
                    level: "warning");

                ConnectionStatus = "Сервер не отвечает";
                IsTestingConnection = false;
            }
        }
    }
}
