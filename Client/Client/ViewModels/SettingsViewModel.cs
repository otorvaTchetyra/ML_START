using Avalonia.Media;
using Client.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Reactive;
using System.Threading.Tasks;

namespace Client.ViewModels
{
	public class SettingsViewModel : ViewModelBase, IRoutableViewModel
    {
        private IConfigurationService _configurationService;
        private IHealthService _healthService;
        private readonly IApiClient _apiClient;
        private NavigationService _navigationService;
        //public IReadOnlyList<UserModel> Users = new List<UserModel>();

        private bool IsInitialized;

        public string? UrlPathSegment => "settings";
        public IScreen HostScreen { get; }

        // Настройки подключения
        public string _urlServerPath;
        public string UrlServerPath {
            get => _urlServerPath;
            set => this.RaiseAndSetIfChanged(ref _urlServerPath, value);
        }
        private string _connectionStatus;
        private bool _isTestingConnection;

        // Параметры нейросети
        private float _confidence = 0.5f;
        private float _iou = 0.45f;

        // UI состояния
        private string _statusMessage;
        private IBrush _statusMessageColor;
        private bool _isStatusVisible;
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
        public ReactiveCommand<Unit, Task> NavigateToMainCommand { get; }
        public ReactiveCommand<Unit, Task> DropDBCommand { get; }
        public SettingsViewModel( IConfigurationService configurationService, IHealthService healthService, IScreen screen, IApiClient apiClient, NavigationService navigationService) 
		{
			_configurationService = configurationService;
            _navigationService = navigationService;
            _healthService = healthService;
            HostScreen = screen;
            _apiClient = apiClient;
            SaveSettingsCommand = ReactiveCommand.CreateFromTask(SaveSettingsAsync);
            TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnectionAsync);
            NavigateToMainCommand = ReactiveCommand.Create(NavigateToMainAsync);
            DropDBCommand = ReactiveCommand.Create(DropDBAsync);
        }

       

        private async Task NavigateToMainAsync()
        {
            await _navigationService.NavigateToMainAsync();
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
            try
            {
                Confidence = float.Parse(config["NeuralNetwork:Confidence"] ?? "0,5");
                Iou = float.Parse(config["NeuralNetwork:Iou"] ?? "0,45");
            }
            catch (Exception ex) 
            {
                Confidence = 0.5f;
                Iou = 0.45f;
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
                _configurationService.SaveSettings(Confidence, Iou);

                var settings = new
                {
                    confidence = Confidence,
                    iou = Iou,
                };


                StatusMessage = "✅ Настройки успешно сохранены!";
                StatusMessageColor = Brushes.Green;

                await Task.Delay(2000);
                IsStatusVisible = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Ошибка сохранения: {ex.Message}";
                StatusMessageColor = Brushes.Red;
            }
        }


        private async Task TestConnectionAsync()
        {
            IsTestingConnection = true;
            ConnectionStatus = "Проверка...";

            try
            {
                var tempClient = new ApiClient(new HttpClient { BaseAddress = new Uri(UrlServerPath) });
                var health = await _healthService.GetHealthStatusAsync();

                ConnectionStatus = "✅ Сервер доступен";
                IsTestingConnection = false;
            }
            catch (Exception)
            {
                ConnectionStatus = "❌ Сервер не отвечает";
                IsTestingConnection = false;
            }
        }
    }

}