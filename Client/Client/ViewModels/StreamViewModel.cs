using Avalonia.Media.Imaging;
using Client.Models;
using Client.Services;
using OpenCvSharp;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace Client.ViewModels
{
    public class StreamViewModel: ViewModelBase, IRoutableViewModel
    {

        private bool IsInitialized;

        public string? UrlPathSegment => "main";
        public IScreen HostScreen { get; }
        private readonly AuthService _authService;
        private readonly NavigationService _navigationService;
        private readonly EventsService _eventsService;
        private readonly StreamService _streamService;
        private CameraCaptureService _cameraService;
        private bool _isCapturing = false;
        private StreamFrame _currentFrameInfo;
        public StreamFrame CurrentFrameInfo
        {
            get => _currentFrameInfo;
            set => this.RaiseAndSetIfChanged(ref _currentFrameInfo, value);
        }
        private Bitmap _currentFrame;
        public Bitmap CurrentFrame
        {
            get => _currentFrame;
            set => this.RaiseAndSetIfChanged(ref _currentFrame, value);
        }

        private ObservableCollection<FeedingEvent> _events;
        public ObservableCollection<FeedingEvent> Events
        {
            get => _events;
            set => this.RaiseAndSetIfChanged(ref _events, value);
        }
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


        public ReactiveCommand<Unit, Unit> GoToSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> GoToMainCommand { get; }

        public ReactiveCommand<Unit, Unit> StartCameraCommand { get; }
        public ReactiveCommand<Unit, Unit> StopCameraCommand { get; }

        public StreamViewModel(IScreen screen, AuthService authService,
            NavigationService navigationService, EventsService eventsService,
            StreamService streamService, CameraCaptureService cameraService)
        {
            HostScreen = screen;
            _authService = authService;
            _navigationService = navigationService;
            _eventsService = eventsService;
            _streamService = streamService;
            _cameraService = cameraService;

            GoToSettingsCommand = ReactiveCommand.CreateFromTask(GoToSettingsAsync);
            GoToMainCommand = ReactiveCommand.CreateFromTask(GoToMainAsync);
            StartCameraCommand = ReactiveCommand.CreateFromTask(StartCameraAsync);
            StopCameraCommand = ReactiveCommand.CreateFromTask(StopCameraAsync);

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
        { }
        private async Task StartCameraAsync()
        {
            if (_isCapturing) return;

            _isCapturing = true;
            await _streamService.StartCameraAsync("0", onFrameReceived: OnStreamFrame); // старт детектов

            _cameraService.OnFrameCaptured += OnFrameCaprured;
            await _cameraService.StartCaptureAsync(0); // старт визуала
        }
        private void OnStreamFrame(StreamFrame frame) // тут надо создавать ивент по полученному кадру с детектами. Его надо в список всех ивентов для журнала
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                StatusText = $"Гранул: {frame.Granule_count} | {frame.Intensity_per_sec:F1}/сек";
                UpdateBoundingBoxes(frame);

                if (frame.Threshold_exceeded || frame.Out_of_schedule)
                {
                    Events.Insert(0, new FeedingEvent
                    {
                        Timestamp = DateTime.Now,
                        GranuleCount = frame.Granule_count,
                        IntensityPerSec = frame.Intensity_per_sec,
                        ThresholdExceeded = frame.Threshold_exceeded,
                        IsOutOfSchedule = frame.Out_of_schedule,
                        StreamFrame = frame
                    });
                }
            });
        }
        private void UpdateBoundingBoxes(StreamFrame frame) // они передают коорды, а тут редакт для вывода на экран
        {
            foreach (DetectionBbox bbox in frame.bboxes)
            {
                bbox.Width = bbox.x2 - bbox.x1;
                bbox.Height = bbox.y2 - bbox.y1;
            }
            CurrentFrameInfo = frame;

        }
        private void OnFrameCaprured(Mat frame)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    // Конвертируем Mat в Bitmap
                    using var stream = new MemoryStream();
                    var imageBytes = frame.ToBytes(".jpg");
                    var bitmap = new Bitmap(new MemoryStream(imageBytes));
                    CurrentFrame = bitmap;
                    frame.Dispose(); // Освобождаем ресурсы
                });
        }
        private void OnDetectionResult(byte[] imageBytes, FeedingEvent feedingEvent)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Events.Insert(0, feedingEvent); // Добавляем в журнал
                StatusText = $"Обнаружено гранул: {feedingEvent.GranuleCount}";
                var bitmap = new Bitmap(new MemoryStream(imageBytes));
                CurrentFrame = bitmap;
            });
        }
        private async Task StopCameraAsync()
        {
            if (!_isCapturing) return;
            _isCapturing = false;

            // ✅ Отписываемся по сохранённым ссылкам
            if (_cameraService != null)
            {
                _cameraService.OnFrameCaptured -= OnFrameCaprured;
                _cameraService.StopCapture();
                _cameraService.Dispose();
            }

            CurrentFrame = null;
            StatusText = "Поток с камеры остановлен";
        }
        private async Task GoToSettingsAsync()
        {
            await _navigationService.NavigateToSettingsAsync();
        }
        private async Task GoToMainAsync()
        {
            await _navigationService.NavigateToMainAsync();
        }
    }
}
