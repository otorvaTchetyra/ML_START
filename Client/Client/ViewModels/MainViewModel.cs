using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Client.Models;
using Client.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Threading.Tasks;
using OpenCvSharp;

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
        private readonly object _videoFrameLock = new();
        private static readonly TimeSpan FrameRenderInterval = TimeSpan.FromMilliseconds(120);
        private static readonly TimeSpan FrameAlertInterval = TimeSpan.FromSeconds(5);

        private string _statusText = "Готов к работе";
        private string _serverStatus = "Проверка подключения...";
        private string _serverStatusColor = "Gray";
        private string _videoPath = string.Empty;
        private string _commentText = string.Empty;
        private bool _isRawVideoVisible = true;
        private bool _isAnnotatedFrameVisible;
        private bool _isJournalRefreshInProgress;
        private bool _isAnalysisActive;
        private int _videoSourceWidth = 0;
        private int _videoSourceHeight = 0;
        private double _videoViewportWidth = 0;
        private double _videoViewportHeight = 0;
        private DateTime _lastAnnotatedFrameAt = DateTime.MinValue;
        private DateTime _lastFrameAlertAt = DateTime.MinValue;
        private Bitmap? _annotatedFrame;
        private VideoCapture? _analysisCapture;
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

        public Bitmap? AnnotatedFrame
        {
            get => _annotatedFrame;
            set => this.RaiseAndSetIfChanged(ref _annotatedFrame, value);
        }

        public bool IsAnnotatedFrameVisible
        {
            get => _isAnnotatedFrameVisible;
            set => this.RaiseAndSetIfChanged(ref _isAnnotatedFrameVisible, value);
        }

        public bool IsRawVideoVisible
        {
            get => _isRawVideoVisible;
            set => this.RaiseAndSetIfChanged(ref _isRawVideoVisible, value);
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

            _journalRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
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
            await LoadDataFastAsync();
            return;

            StatusText = "Загрузка данных...";

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

            await LoadJournalAsync();

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
                if (StatusText == "Загрузка данных...")
                    StatusText = "Ошибка загрузки данных";
            }
        }

        private async Task LoadDataFastAsync()
        {
            StatusText = "Загрузка данных...";

            await Task.WhenAll(
                LoadEventsFastAsync(),
                LoadJournalAsync(),
                LoadServerStatusFastAsync());
        }

        private async Task LoadEventsFastAsync()
        {
            try
            {
                var events = await _eventsService.GetEventsAsync();
                Events.Clear();
                if (events != null)
                {
                    foreach (var e in events)
                        Events.Add(e);
                }

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
            catch
            {
            }
            finally
            {
                _isJournalRefreshInProgress = false;
            }
        }

        private bool ShouldReplaceJournalEntries(IReadOnlyList<JournalEntry> entries)
        {
            if (JournalEntries.Count != entries.Count)
                return true;

            for (var i = 0; i < entries.Count; i++)
            {
                var current = JournalEntries[i];
                var next = entries[i];
                if (current.Id != next.Id
                    || current.Timestamp != next.Timestamp
                    || current.Level != next.Level
                    || current.Message != next.Message
                    || current.Comment != next.Comment)
                {
                    return true;
                }
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
            ClearAnnotatedFrame();
        }

        public async Task StartVideoAndAnalysisAsync()
        {
            if (string.IsNullOrWhiteSpace(VideoPath) || _isAnalysisActive)
                return;

            try
            {
                ClearAnnotatedFrame();
                ResetAnalysisCapture(VideoPath);
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
                ClearAnnotatedFrame();
                ResetAnalysisCapture(VideoPath);
                StatusText = $"Загружено: {System.IO.Path.GetFileName(VideoPath)}";

                try
                {
                    using var capture = new VideoCapture(VideoPath);
                    if (capture.IsOpened())
                    {
                        _videoSourceWidth = (int)Math.Max(1, capture.FrameWidth);
                        _videoSourceHeight = (int)Math.Max(1, capture.FrameHeight);
                    }
                }
                catch
                {
                    _videoSourceWidth = 0;
                    _videoSourceHeight = 0;
                }

                await _journalService.RecordAsync(
                    eventCode: "video_opened",
                    message: $"Открыт видеофайл {System.IO.Path.GetFileName(VideoPath)}",
                    source: "stream",
                    action: "open_video",
                    level: "info",
                    detailsJson: $"{{\"path\":\"{VideoPath.Replace("\\", "\\\\")}\"}}",
                    usernameSnapshot: _authService.CurrentUser?.Username);

                _isAnalysisActive = false;
                await StartVideoAndAnalysisAsync();
                await LoadJournalAsync();
                return;

                try
                {
                    await _streamService.SendVideoAsync(VideoPath, OnStreamFrame);
                    StatusText = "Анализ видео запущен";
                }
                catch (Exception ex)
                {
                    StatusText = $"Не удалось запустить анализ видео: {ex.Message}";
                }

                await LoadJournalAsync();
            }
        }

        private void OnStreamFrame(StreamFrame frame)
        {
            Bitmap? annotatedFrame = null;
            var now = DateTime.UtcNow;
            if (now - _lastAnnotatedFrameAt >= FrameRenderInterval)
            {
                _lastAnnotatedFrameAt = now;
                annotatedFrame = BuildAnnotatedFrame(frame);
            }

            Dispatcher.UIThread.Post(() =>
            {
                foreach (var bbox in frame.bboxes)
                {
                    bbox.Width = bbox.x2 - bbox.x1;
                    bbox.Height = bbox.y2 - bbox.y1;
                }

                CurrentFrameInfo = frame;
                if (annotatedFrame != null)
                {
                    AnnotatedFrame = annotatedFrame;
                    IsAnnotatedFrameVisible = true;
                    IsRawVideoVisible = false;
                }

                RebuildOverlayDetections(frame);
                StatusText = $"Кадр {frame.Frame_index}: гранул {frame.Granule_count}, детектов {frame.bboxes.Count}";
            });

            if (frame.Threshold_exceeded || frame.Out_of_schedule)
            {
                if (now - _lastFrameAlertAt >= FrameAlertInterval)
                {
                    _lastFrameAlertAt = now;
                    _ = RecordFrameAlertAsync(frame);
                }
            }
        }

        private Bitmap? BuildAnnotatedFrame(StreamFrame frame)
        {
            if (string.IsNullOrWhiteSpace(VideoPath))
                return null;

            lock (_videoFrameLock)
            {
                if (_analysisCapture == null || !_analysisCapture.IsOpened())
                    ResetAnalysisCapture(VideoPath);

                if (_analysisCapture == null || !_analysisCapture.IsOpened())
                    return null;

                _analysisCapture.Set(VideoCaptureProperties.PosFrames, Math.Max(0, frame.Frame_index));

                using var mat = new Mat();
                if (!_analysisCapture.Read(mat) || mat.Empty())
                    return null;

                foreach (var bbox in frame.bboxes)
                {
                    var left = ClampToFrame(Math.Min(bbox.x1, bbox.x2), mat.Width);
                    var top = ClampToFrame(Math.Min(bbox.y1, bbox.y2), mat.Height);
                    var right = ClampToFrame(Math.Max(bbox.x1, bbox.x2), mat.Width);
                    var bottom = ClampToFrame(Math.Max(bbox.y1, bbox.y2), mat.Height);

                    var width = Math.Max(2, right - left);
                    var height = Math.Max(2, bottom - top);
                    Cv2.Rectangle(mat, new Rect(left, top, width, height), new Scalar(0, 0, 255), 2);

                    var label = bbox.confidence.ToString("0.00", CultureInfo.InvariantCulture);
                    var labelOrigin = new Point(left, Math.Max(12, top - 4));
                    Cv2.PutText(mat, label, labelOrigin, HersheyFonts.HersheySimplex, 0.45, new Scalar(0, 0, 255), 1);
                }

                return new Bitmap(new MemoryStream(mat.ToBytes(".jpg")));
            }
        }

        private static int ClampToFrame(double value, int limit)
        {
            if (limit <= 1)
                return 0;

            return (int)Math.Clamp(Math.Round(value), 0, limit - 1);
        }

        private void ResetAnalysisCapture(string path)
        {
            lock (_videoFrameLock)
            {
                _analysisCapture?.Release();
                _analysisCapture?.Dispose();
                _analysisCapture = new VideoCapture(path);
            }
        }

        private void ClearAnnotatedFrame()
        {
            _lastAnnotatedFrameAt = DateTime.MinValue;
            AnnotatedFrame = null;
            IsAnnotatedFrameVisible = false;
            IsRawVideoVisible = true;
        }

        private async Task RecordFrameAlertAsync(StreamFrame frame)
        {
            var reason = frame.Threshold_exceeded && frame.Out_of_schedule
                ? "превышен порог и появление вне расписания"
                : frame.Threshold_exceeded
                    ? "превышен порог"
                    : "анализ вне расписания";

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

        public void UpdateVideoViewport(double width, double height)
        {
            if (width <= 0 || height <= 0)
                return;

            _videoViewportWidth = width;
            _videoViewportHeight = height;
            RebuildOverlayDetections(CurrentFrameInfo);
        }

        private void RebuildOverlayDetections(StreamFrame frame)
        {
            if (_videoViewportWidth <= 0 || _videoViewportHeight <= 0 || _videoSourceWidth <= 0 || _videoSourceHeight <= 0)
            {
                OverlayDetections.Clear();
                return;
            }

            var scale = Math.Min(_videoViewportWidth / _videoSourceWidth, _videoViewportHeight / _videoSourceHeight);
            var contentWidth = _videoSourceWidth * scale;
            var contentHeight = _videoSourceHeight * scale;
            var offsetX = (_videoViewportWidth - contentWidth) / 2.0;
            var offsetY = (_videoViewportHeight - contentHeight) / 2.0;

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

        private async Task GoToSettingsAsync()
        {
            await _navigationService.NavigateToSettingsAsync();
        }

        private async Task GoToStreamAsync()
        {
            await _navigationService.NavigateToStreamAsync();
        }

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
