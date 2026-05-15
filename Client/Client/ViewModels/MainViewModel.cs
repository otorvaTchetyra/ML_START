using Avalonia.Controls;
using Avalonia.Threading;
using Client.Models;
using Client.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
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
        private readonly JournalService _journalService;
        private readonly StreamService _streamService;
        private readonly DispatcherTimer _journalRefreshTimer;
        private static readonly TimeSpan FrameAlertInterval = TimeSpan.FromSeconds(5);

        private string _statusText = "Готов к работе";
        private string _serverStatus = "Проверка подключения...";
        private string _serverStatusColor = "Gray";
        private string _videoPath = string.Empty;
        private string _commentText = string.Empty;
        private bool _isJournalRefreshInProgress;
        private bool _isAnalysisActive;
        private int _videoSourceWidth = 0;
        private int _videoSourceHeight = 0;
        private double _videoViewportWidth = 0;
        private double _videoViewportHeight = 0;
        private DateTime _lastFrameAlertAt = DateTime.MinValue;
        private FeedingEvent? _selectedEvent;
        private JournalEntry? _selectedJournalEntry;
        private StreamFrame _currentFrameInfo = new();
        private ObservableCollection<OverlayDetection> _overlayDetections = new();
        private ObservableCollection<FeedingEvent> _events = new();
        private ObservableCollection<JournalEntry> _journalEntries = new();

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

        public ObservableCollection<JournalEntry> JournalEntries
        {
            get => _journalEntries;
            set => this.RaiseAndSetIfChanged(ref _journalEntries, value);
        }

        public JournalEntry? SelectedJournalEntry
        {
            get => _selectedJournalEntry;
            set => this.RaiseAndSetIfChanged(ref _selectedJournalEntry, value);
        }

        public StreamFrame CurrentFrameInfo
        {
            get => _currentFrameInfo;
            set => this.RaiseAndSetIfChanged(ref _currentFrameInfo, value);
        }

        public ObservableCollection<OverlayDetection> OverlayDetections
        {
            get => _overlayDetections;
            set => this.RaiseAndSetIfChanged(ref _overlayDetections, value);
        }

        public bool IsAdmin => _authService.IsAdmin;
        public bool HasVideoSourceSize => _videoSourceWidth > 0;

        public ReactiveCommand<Unit, Unit> OpenVideoCommand { get; }
        public ReactiveCommand<Unit, Unit> GoToSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> GoToStreamCommand { get; }
        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }
        public ReactiveCommand<Unit, Unit> StopStreamCommand { get; }
        public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
        public ReactiveCommand<Unit, Unit> AddCommentCommand { get; }

        public MainViewModel(
            IScreen screen,
            AuthService authService,
            NavigationService navigationService,
            EventsService eventsService,
            JournalService journalService,
            StreamService streamService,
            CameraCaptureService cameraService)
        {
            HostScreen = screen;
            _authService = authService;
            _navigationService = navigationService;
            _eventsService = eventsService;
            _journalService = journalService;
            _streamService = streamService;

            _journalRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _journalRefreshTimer.Tick += async (_, _) => await LoadJournalAsync();

            OpenVideoCommand = ReactiveCommand.CreateFromTask(OpenVideoAsync);
            GoToSettingsCommand = ReactiveCommand.CreateFromTask(GoToSettingsAsync);
            GoToStreamCommand = ReactiveCommand.CreateFromTask(GoToStreamAsync);
            LogoutCommand = ReactiveCommand.CreateFromTask(LogoutAsync);
            StopStreamCommand = ReactiveCommand.CreateFromTask(StopStreamAsync);
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadData);
            AddCommentCommand = ReactiveCommand.CreateFromTask(AddCommentAsync);
        }

        public async Task InitializeAsync()
        {
            if (!IsInitialized)
            {
                IsInitialized = true;
                await LoadData();
                _journalRefreshTimer.Start();
            }
        }

        private async Task LoadData()
        {
            StatusText = "Загрузка данных...";
            await Task.WhenAll(LoadEventsFastAsync(), LoadJournalAsync(), LoadServerStatusFastAsync());
        }

        private async Task LoadEventsFastAsync()
        {
            try
            {
                var events = await _eventsService.GetEventsAsync();
                Events.Clear();
                if (events != null)
                    foreach (var e in events)
                        Events.Add(e);
                StatusText = $"Загружено событий: {Events.Count}";
            }
            catch
            {
                StatusText = "Ошибка загрузки событий";
            }
        }

        private async Task LoadServerStatusFastAsync()
        {
            try
            {
                var status = await _streamService.GetStatusAsync();
                if (status != null)
                {
                    ServerStatus = "Успешное подключение к серверу";
                    ServerStatusColor = "Green";
                }
            }
            catch
            {
                ServerStatus = "Сервер недоступен";
                ServerStatusColor = "Red";
            }
        }

        private async Task LoadJournalAsync()
        {
            if (_isJournalRefreshInProgress)
                return;
            _isJournalRefreshInProgress = true;
            try
            {
                var entries = await _journalService.GetEntriesAsync(limit: 200);
                if (!ShouldReplaceJournalEntries(entries))
                    return;
                var selectedId = SelectedJournalEntry?.Id;
                JournalEntries = new ObservableCollection<JournalEntry>(entries);
                if (selectedId.HasValue)
                    SelectedJournalEntry = JournalEntries.FirstOrDefault(x => x.Id == selectedId.Value);
            }
            catch { }
            finally { _isJournalRefreshInProgress = false; }
        }

        private bool ShouldReplaceJournalEntries(IReadOnlyList<JournalEntry> entries)
        {
            if (JournalEntries.Count != entries.Count)
                return true;
            for (var i = 0; i < entries.Count; i++)
            {
                var c = JournalEntries[i];
                var n = entries[i];
                if (c.Id != n.Id || c.Timestamp != n.Timestamp || c.Level != n.Level
                    || c.Message != n.Message || c.Comment != n.Comment)
                    return true;
            }
            return false;
        }

        public async Task LogPlaybackActionAsync(string action, string message)
        {
            await _journalService.RecordAsync(
                eventCode: "custom",
                message: message,
                source: "player",
                action: action,
                level: "info",
                usernameSnapshot: _authService.CurrentUser?.Username);
            await LoadJournalAsync();
        }

        private async Task AddCommentAsync()
        {
            if (SelectedJournalEntry == null || string.IsNullOrWhiteSpace(CommentText))
                return;
            try
            {
                await _journalService.AddCommentAsync(SelectedJournalEntry.Id, CommentText);
                await _journalService.RecordAsync(
                    eventCode: "comment_added",
                    message: $"Добавлен комментарий к записи журнала {SelectedJournalEntry.Id}",
                    source: "journal",
                    action: "add_comment",
                    level: "info",
                    entityType: "journal_entry",
                    entityId: SelectedJournalEntry.Id,
                    comment: CommentText,
                    usernameSnapshot: _authService.CurrentUser?.Username);
                CommentText = string.Empty;
                await LoadJournalAsync();
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
                _isAnalysisActive = false;
                await _journalService.RecordAsync(
                    eventCode: "stream_stopped",
                    message: "Анализ потока остановлен вручную",
                    source: "stream",
                    action: "stop",
                    level: "info",
                    usernameSnapshot: _authService.CurrentUser?.Username);
                StatusText = "Анализ остановлен";
                await LoadJournalAsync();
            }
            catch
            {
                StatusText = "Ошибка остановки анализа";
            }
        }

        public async Task PauseAnalysisAsync()
        {
            try
            {
                await _streamService.StopAsync();
                _isAnalysisActive = false;
                await _journalService.RecordAsync(
                    eventCode: "stream_paused",
                    message: "Анализ потока поставлен на паузу",
                    source: "player",
                    action: "pause",
                    level: "info",
                    usernameSnapshot: _authService.CurrentUser?.Username);
                StatusText = "Анализ поставлен на паузу";
                await LoadJournalAsync();
            }
            catch
            {
                StatusText = "Ошибка паузы анализа";
            }
        }

        public async Task StopVideoAndAnalysisAsync()
        {
            await StopStreamAsync();
            OverlayDetections.Clear();
            CurrentFrameInfo = new StreamFrame();
        }

        public async Task StartVideoAndAnalysisAsync()
        {
            if (string.IsNullOrWhiteSpace(VideoPath) || _isAnalysisActive)
                return;
            try
            {
                OverlayDetections.Clear();
                var (fw, fh) = VideoDimensionReader.TryRead(VideoPath);
                _videoSourceWidth = fw;
                _videoSourceHeight = fh;
                await _streamService.SendVideoAsync(VideoPath, OnStreamFrame);
                _isAnalysisActive = true;
                StatusText = "Анализ видео запущен";
            }
            catch (Exception ex)
            {
                _isAnalysisActive = false;
                StatusText = $"Не удалось запустить анализ видео: {ex.Message}";
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
                OverlayDetections.Clear();
                _videoSourceWidth = 0;
                _videoSourceHeight = 0;
                StatusText = $"Загружено: {Path.GetFileName(VideoPath)}";

                await _journalService.RecordAsync(
                    eventCode: "video_opened",
                    message: $"Открыт видеофайл {Path.GetFileName(VideoPath)}",
                    source: "stream",
                    action: "open_video",
                    level: "info",
                    detailsJson: $"{{\"path\":\"{VideoPath.Replace("\\", "\\\\")}\"}}",
                    usernameSnapshot: _authService.CurrentUser?.Username);

                _isAnalysisActive = false;
                await StartVideoAndAnalysisAsync();
                await LoadJournalAsync();
            }
        }

        private void OnStreamFrame(StreamFrame frame)
        {
            var now = DateTime.UtcNow;
            bool alert = (frame.Threshold_exceeded || frame.Out_of_schedule)
                         && now - _lastFrameAlertAt >= FrameAlertInterval;
            if (alert)
                _lastFrameAlertAt = now;

            Dispatcher.UIThread.Post(() =>
            {
                foreach (var bbox in frame.bboxes)
                {
                    bbox.Width = bbox.x2 - bbox.x1;
                    bbox.Height = bbox.y2 - bbox.y1;
                }

                if (_videoSourceWidth == 0 && frame.Source_width > 0)
                {
                    _videoSourceWidth = frame.Source_width;
                    _videoSourceHeight = frame.Source_height;
                }

                CurrentFrameInfo = frame;
                RebuildOverlayDetections(frame);
                StatusText = $"Кадр {frame.Frame_index}: гранул {frame.Granule_count}, детектов {frame.bboxes.Count}, оверлей {OverlayDetections.Count}, вьюпорт {_videoViewportWidth:F0}x{_videoViewportHeight:F0}, источник {_videoSourceWidth}x{_videoSourceHeight}";
            });

            if (alert)
                _ = RecordFrameAlertAsync(frame);
        }

        public void SetVideoSourceSize(int width, int height)
        {
            if (width <= 0 || height <= 0) return;
            _videoSourceWidth = width;
            _videoSourceHeight = height;
            RebuildOverlayDetections(CurrentFrameInfo);
        }

        public void UpdateVideoViewport(double width, double height)
        {
            if (width <= 0 || height <= 0) return;
            _videoViewportWidth = width;
            _videoViewportHeight = height;
            RebuildOverlayDetections(CurrentFrameInfo);
        }

        private void RebuildOverlayDetections(StreamFrame frame)
        {
            if (_videoViewportWidth <= 0 || _videoViewportHeight <= 0
                || _videoSourceWidth <= 0 || _videoSourceHeight <= 0)
            {
                OverlayDetections.Clear();
                return;
            }

            var scale = Math.Min(_videoViewportWidth / _videoSourceWidth, _videoViewportHeight / _videoSourceHeight);
            var offsetX = (_videoViewportWidth - _videoSourceWidth * scale) / 2.0;
            var offsetY = (_videoViewportHeight - _videoSourceHeight * scale) / 2.0;

            var overlays = new ObservableCollection<OverlayDetection>();
            foreach (var bbox in frame.bboxes)
            {
                overlays.Add(new OverlayDetection
                {
                    Left = offsetX + bbox.x1 * scale,
                    Top = offsetY + bbox.y1 * scale,
                    Width = Math.Max(1, (bbox.x2 - bbox.x1) * scale),
                    Height = Math.Max(1, (bbox.y2 - bbox.y1) * scale),
                    Label = $"Conf: {bbox.confidence:0.00}"
                });
            }
            OverlayDetections = overlays;
        }

        private async Task RecordFrameAlertAsync(StreamFrame frame)
        {
            var reason = frame.Threshold_exceeded && frame.Out_of_schedule
                ? "превышен порог и появление вне расписания"
                : frame.Threshold_exceeded ? "превышен порог" : "анализ вне расписания";

            await _journalService.RecordAsync(
                eventCode: "frame_detection",
                message: $"Кадр {frame.Frame_index}: {reason}. гранул: {frame.Granule_count}, детектов: {frame.bboxes.Count}",
                source: "stream",
                action: "frame_analysis",
                level: "warning",
                detailsJson: JsonSerializer.Serialize(new
                {
                    frame_index = frame.Frame_index,
                    granule_count = frame.Granule_count,
                    intensity_per_sec = frame.Intensity_per_sec,
                    intensity_per_min = frame.Intensity_per_min,
                    threshold_exceeded = frame.Threshold_exceeded,
                    out_of_schedule = frame.Out_of_schedule,
                    bboxes = frame.bboxes
                }),
                usernameSnapshot: _authService.CurrentUser?.Username);

            await LoadJournalAsync();
        }

        private async Task GoToSettingsAsync() => await _navigationService.NavigateToSettingsAsync();
        private async Task GoToStreamAsync() => await _navigationService.NavigateToStreamAsync();

        private async Task LogoutAsync()
        {
            _journalRefreshTimer.Stop();
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
}
