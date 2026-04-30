using Avalonia.Controls;
using Client.Models;
using Client.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

namespace Client.ViewModels
{
    public class MainViewModel : ViewModelBase, IRoutableViewModel
    {
        private bool IsInitialized;
        public string? UrlPathSegment => "main";
        public IScreen HostScreen { get; }

        private readonly AuthService _authService;
        private readonly NavigationService _navigationService;
        private readonly EventsService _eventsService;
        private readonly StreamService _streamService;

        private string _statusText = "Готов к работе";
        private string _serverStatus = "Подключение...";
        private string _serverStatusColor = "Gray";
        private string _videoPath = string.Empty;
        private string _commentText = string.Empty;
        private FeedingEvent? _selectedEvent;
        private ObservableCollection<FeedingEvent> _events = new();

        public string StatusText
        {
            get => _statusText;
            set => this.RaiseAndSetIfChanged(ref _statusText, value);
        }

        public string ServerStatus
        {
            get => _serverStatus;
            set => this.RaiseAndSetIfChanged(ref _serverStatus, value);
        }

        public string ServerStatusColor
        {
            get => _serverStatusColor;
            set => this.RaiseAndSetIfChanged(ref _serverStatusColor, value);
        }

        public string VideoPath
        {
            get => _videoPath;
            set => this.RaiseAndSetIfChanged(ref _videoPath, value);
        }

        public string CommentText
        {
            get => _commentText;
            set => this.RaiseAndSetIfChanged(ref _commentText, value);
        }

        public FeedingEvent? SelectedEvent
        {
            get => _selectedEvent;
            set => this.RaiseAndSetIfChanged(ref _selectedEvent, value);
        }

        public ObservableCollection<FeedingEvent> Events
        {
            get => _events;
            set => this.RaiseAndSetIfChanged(ref _events, value);
        }

        public ReactiveCommand<Unit, Unit> OpenVideoCommand { get; }
        public ReactiveCommand<Unit, Unit> GoToSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> StopStreamCommand { get; }
        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
        public ReactiveCommand<Unit, Unit> AddCommentCommand { get; }

        public MainViewModel(IScreen screen, AuthService authService,
            NavigationService navigationService, EventsService eventsService,
            StreamService streamService)
        {
            HostScreen = screen;
            _authService = authService;
            _navigationService = navigationService;
            _eventsService = eventsService;
            _streamService = streamService;

            OpenVideoCommand = ReactiveCommand.CreateFromTask(OpenVideoAsync);
            GoToSettingsCommand = ReactiveCommand.CreateFromTask(GoToSettingsAsync);
            StopStreamCommand = ReactiveCommand.CreateFromTask(StopStreamAsync);
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadData);
            AddCommentCommand = ReactiveCommand.CreateFromTask(AddCommentAsync);
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
            try
            {
                StatusText = "Загрузка данных...";

                // Загружаем события
                var events = await _eventsService.GetEventsAsync();
                Events.Clear();
                if (events != null)
                    foreach (var e in events)
                        Events.Add(e);

                // Проверяем статус сервера
                var status = await _streamService.GetStatusAsync();
                if (status != null)
                {
                    ServerStatus = "Сервер: онлайн";
                    ServerStatusColor = "Green";
                }

                StatusText = $"Загружено событий: {Events.Count}";
            }
            catch
            {
                ServerStatus = "Сервер: недоступен";
                ServerStatusColor = "Red";
                StatusText = "Ошибка загрузки данных";
            }
        }

        private async Task AddCommentAsync()
        {
            if (SelectedEvent == null || string.IsNullOrWhiteSpace(CommentText))
                return;

            try
            {
                await _eventsService.AddCommentAsync(SelectedEvent.Id, CommentText);
                CommentText = string.Empty;
                await LoadData();
            }
            catch
            {
                StatusText = "Ошибка при добавлении комментария";
            }
        }

        private async Task StopStreamAsync()
        {
            try
            {
                await _streamService.StopAsync();
                StatusText = "Анализ остановлен";
            }
            catch
            {
                StatusText = "Ошибка остановки анализа";
            }
        }

        private async Task OpenVideoAsync()
        {
            var topLevel = TopLevel.GetTopLevel(App.MainWindow);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Выберите видеофайл",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Видеофайлы")
                        {
                            Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov" }
                        }
                    }
                });

            if (files.Count > 0)
            {
                VideoPath = files[0].Path.LocalPath;
                StatusText = $"Загружено: {System.IO.Path.GetFileName(VideoPath)}";
            }
        }

        private async Task GoToSettingsAsync()
        {
            await _navigationService.NavigateToSettingsAsync();
        }
    }
}