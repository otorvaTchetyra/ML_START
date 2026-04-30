using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Client.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace Client.ViewModels
{
    public class MainViewModel: ViewModelBase, IRoutableViewModel
    {
        private bool IsInitialized;

        public string? UrlPathSegment => "main";
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

        private string _videoPath;
        //private MediaSource _videoSource;
        private double _volume = 0.8;
        private bool _autoPlay = true;
        private string _statusText = "Готов к работе";
        private ObservableCollection<int> _detections = new();
        private int _selectedDetection;
        public string VideoPath
        {
            get => _videoPath;
            set => this.RaiseAndSetIfChanged(ref _videoPath, value);
        }

        public double Volume
        {
            get => _volume;
            set => this.RaiseAndSetIfChanged(ref _volume, value);
        }

        public bool AutoPlay
        {
            get => _autoPlay;
            set => this.RaiseAndSetIfChanged(ref _autoPlay, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => this.RaiseAndSetIfChanged(ref _statusText, value);
        }

        public ObservableCollection<int> Detections
        {
            get => _detections;
            set => this.RaiseAndSetIfChanged(ref _detections, value);
        }

        public int SelectedDetection
        {
            get => _selectedDetection;
            set => this.RaiseAndSetIfChanged(ref _selectedDetection, value);
        }
        public ReactiveCommand<Unit, Unit> OpenVideoCommand { get; }

        public ReactiveCommand<Unit, Unit> GoToSettingsCommand { get; }

        public MainViewModel(IScreen screen, AuthService authService, NavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
            HostScreen = screen;

            OpenVideoCommand = ReactiveCommand.CreateFromTask(OpenVideoAsync);
            GoToSettingsCommand = ReactiveCommand.CreateFromTask(GoToSettingsAsync);
        }

        private async Task OpenVideoAsync()
        {

            var topLevel = TopLevel.GetTopLevel(App.MainWindow);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите видеофайл",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Видеофайлы")
                    {
                        Patterns = new[] { "*.mp4", "*.avi", "*.mkv", "*.mov", "*.wmv", "*.flv", "*.webm", "*.m4v" }
                    }
                }
            });

            if (files.Count > 0)
            {
                var selectedFile = files[0].Path.LocalPath;
                VideoPath = selectedFile;
                //VideoSource = new UriSource($"file:///{selectedFile.Replace('\\', '/')}");
                StatusText = $"Загружено видео: {System.IO.Path.GetFileName(selectedFile)}";
            }
        }
        public void AddDetection(string className, float confidence, TimeSpan timestamp)
        {
            /*var detection = new DetectionItem
            {
                ClassName = className,
                Confidence = confidence,
                Timestamp = timestamp,
                ConfidenceColor = confidence > 0.7 ? "#4CAF50" : confidence > 0.4 ? "#FFC107" : "#F44336"
            };
            Detections.Add(detection);
            StatusText = $"Обнаружен: {className} ({confidence:P0}) на {timestamp:hh\\:mm\\:ss}";*/
        }

        public void ClearDetections()
        {
            _detections.Clear();
            _statusText = "Журнал очищен";
        }
        private async Task GoToSettingsAsync()
        {
            await _navigationService.NavigateToSettingsAsync();
        }
    }
}

