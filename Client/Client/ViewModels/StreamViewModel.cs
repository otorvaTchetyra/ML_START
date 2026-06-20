using Avalonia.Media.Imaging;
using Client.Models;
using Client.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Client.ViewModels
{
    public class StreamViewModel : ViewModelBase, IRoutableViewModel
    {
        private bool IsInitialized;

        public string? UrlPathSegment => "main";
        public IScreen HostScreen { get; }

        private readonly AuthService _authService;
        private readonly NavigationService _navigationService;
        private readonly EventsService _eventsService;
        private readonly JournalService _journalService;
        private readonly StreamService _streamService;
        private readonly CameraCaptureService _cameraService;
        private bool _isCapturing;
        private CancellationTokenSource? _mjpegCts;
        private int _streamVersion;

        private Bitmap? _currentFrame;
        public Bitmap? CurrentFrame
        {
            get => _currentFrame;
            set => this.RaiseAndSetIfChanged(ref _currentFrame, value);
        }

        private const string Channel1Url = "https://www.youtube.com/watch?v=7i8ARjIeM2k";
        private const string Channel2Url = "https://sochi.camera/vse-kamery/cam-40/";

        private string _channel3Source = "";
        public string Channel3Source
        {
            get => _channel3Source;
            set => this.RaiseAndSetIfChanged(ref _channel3Source, value);
        }

        private int _activeChannel = 0;
        public int ActiveChannel
        {
            get => _activeChannel;
            set => this.RaiseAndSetIfChanged(ref _activeChannel, value);
        }

        private StreamFrame _currentFrameInfo = new();
        public StreamFrame CurrentFrameInfo
        {
            get => _currentFrameInfo;
            set => this.RaiseAndSetIfChanged(ref _currentFrameInfo, value);
        }

        private ObservableCollection<FeedingEvent> _events = new();
        public ObservableCollection<FeedingEvent> Events
        {
            get => _events;
            set => this.RaiseAndSetIfChanged(ref _events, value);
        }

        public bool IsAdmin => _authService.IsAdmin;

        private string _statusText = "Готов к работе";
        private string _serverStatus = "Подключение...";
        private string _serverStatusColor = "Gray";

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

        private string _detectionMode = "granule";
        public string DetectionMode
        {
            get => _detectionMode;
            set => this.RaiseAndSetIfChanged(ref _detectionMode, value);
        }

        public ReactiveCommand<Unit, Unit> GoToSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> GoToStatisticsCommand { get; }
        public ReactiveCommand<Unit, Unit> GoToMainCommand { get; }
        public ReactiveCommand<Unit, Unit> StartChannel1Command { get; }
        public ReactiveCommand<Unit, Unit> StartChannel2Command { get; }
        public ReactiveCommand<Unit, Unit> StartChannel3Command { get; }
        public ReactiveCommand<Unit, Unit> StopCameraCommand { get; }
        public ReactiveCommand<string, Unit> SetDetectionModeCommand { get; }

        public StreamViewModel(
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
            _cameraService = cameraService;

            GoToSettingsCommand = ReactiveCommand.CreateFromTask(GoToSettingsAsync);
            GoToStatisticsCommand = ReactiveCommand.CreateFromTask(GoToStatsAsync);
            GoToMainCommand = ReactiveCommand.CreateFromTask(GoToMainAsync);
            StartChannel1Command = ReactiveCommand.CreateFromTask(() => StartStreamAsync(Channel1Url, 1));
            StartChannel2Command = ReactiveCommand.CreateFromTask(() => StartStreamAsync(Channel2Url, 2));
            StartChannel3Command = ReactiveCommand.CreateFromTask(() => StartStreamAsync(Channel3Source, 3));
            StopCameraCommand = ReactiveCommand.CreateFromTask(StopCameraAsync);
            SetDetectionModeCommand = ReactiveCommand.CreateFromTask<string>(SetDetectionModeAsync);
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

        private async Task StartStreamAsync(string source, int channel)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                StatusText = $"Канал {channel}: укажите источник";
                return;
            }

            _isCapturing = false;
            _mjpegCts?.Cancel();
            _mjpegCts = null;

            var myVersion = ++_streamVersion;

            _isCapturing = true;
            ActiveChannel = channel;

            _ = _journalService.RecordAsync(
                eventCode: "stream_started",
                message: $"Запущен анализ потока, канал {channel}",
                source: "stream",
                action: "start",
                level: "info");

            try
            {
                await _streamService.StartCameraAsync(source, OnStreamFrame, onStreamEnded: () =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        _isCapturing = false;
                        ActiveChannel = 0;
                        _mjpegCts?.Cancel();
                        _mjpegCts = null;
                        StatusText = "Поток завершён";
                    });
                });
            }
            catch
            {
                _isCapturing = false;
                ActiveChannel = 0;
                StatusText = "Не удалось подключиться к потоку";
                return;
            }

            _mjpegCts = new CancellationTokenSource();
            var token = _mjpegCts.Token;
            _ = Task.Run(async () =>
            {
                await Task.Delay(500, token);
                await _cameraService.StartMjpegAsync(frame =>
                {
                    if (_streamVersion == myVersion)
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => CurrentFrame = frame);
                }, token);
            }, token);
        }

        private void OnStreamFrame(StreamFrame frame)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                StatusText = $"Гранул: {frame.Granule_count} | {frame.Intensity_per_sec:F1}/сек";

                foreach (var bbox in frame.bboxes)
                {
                    bbox.Width = bbox.x2 - bbox.x1;
                    bbox.Height = bbox.y2 - bbox.y1;
                }

                CurrentFrameInfo = frame;

                if (frame.Threshold_exceeded || frame.Out_of_schedule)
                {
                    Events.Insert(0, new FeedingEvent
                    {
                        Timestamp = DateTime.Now,
                        GranuleCount = frame.Granule_count,
                        IntensityPerSec = frame.Intensity_per_sec,
                        IntensityPerMin = (float)frame.Intensity_per_min,
                        ThresholdExceeded = frame.Threshold_exceeded,
                        IsOutOfSchedule = frame.Out_of_schedule,
                        StreamFrame = frame
                    });
                }
            });

            if (frame.Threshold_exceeded || frame.Out_of_schedule)
            {
                _ = _journalService.RecordAsync(
                    eventCode: frame.Threshold_exceeded ? "threshold_exceeded" : "out_of_schedule",
                    message: $"Обнаружено событие: гранул {frame.Granule_count}, интенсивность {frame.Intensity_per_sec:F1}/сек",
                    source: "detection",
                    action: "frame_analysis",
                    level: "warning",
                    entityType: "stream_frame",
                    detailsJson: $"{{\"frameIndex\":{frame.Frame_index},\"granuleCount\":{frame.Granule_count},\"intensityPerSec\":{frame.Intensity_per_sec.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"intensityPerMin\":{frame.Intensity_per_min.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"thresholdExceeded\":{frame.Threshold_exceeded.ToString().ToLowerInvariant()},\"outOfSchedule\":{frame.Out_of_schedule.ToString().ToLowerInvariant()}}}");
            }
        }

        private async Task StopCameraAsync()
        {
            if (!_isCapturing)
                return;

            _isCapturing = false;
            ActiveChannel = 0;
            _mjpegCts?.Cancel();
            _mjpegCts = null;
            _cameraService.StopCapture();

            await _streamService.StopAsync();

            _ = _journalService.RecordAsync(
                eventCode: "stream_stopped",
                message: "Остановлен анализ потока",
                source: "stream",
                action: "stop",
                level: "info");

            StatusText = "Поток остановлен";
        }

        private async Task SetDetectionModeAsync(string mode)
        {
            DetectionMode = mode;
            await _streamService.SetDetectionModeAsync(mode);
        }

        private async Task GoToSettingsAsync()
        {
            await _navigationService.NavigateToSettingsAsync();
        }

        private async Task GoToMainAsync()
        {
            await _navigationService.NavigateToMainAsync();
        }

        private async Task GoToStatsAsync()
        {
            await _navigationService.NavigateToStatisticsAsync();
        }
    }
}
