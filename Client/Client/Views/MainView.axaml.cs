using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Client.ViewModels;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Client;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    private INotifyPropertyChanged? _boundViewModel;
    private bool _wasStopped;

    public MainView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BindViewModel();
    }

    private void BindViewModel()
    {
        if (_boundViewModel != null)
            _boundViewModel.PropertyChanged -= ViewModelOnPropertyChanged;

        _boundViewModel = DataContext as INotifyPropertyChanged;
        if (_boundViewModel != null)
            _boundViewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private async void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainViewModel vm || e.PropertyName != nameof(MainViewModel.VideoPath))
            return;

        if (string.IsNullOrWhiteSpace(vm.VideoPath))
            return;

        _wasStopped = false;
        await Task.Delay(200);
        VideoPlayer.MediaPlayerViewModel?.Play();
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel startVm)
        {
            await RestartStoppedPlayerAsync(startVm);
            await startVm.StartVideoAndAnalysisAsync();
        }

        VideoPlayer.MediaPlayerViewModel?.Play();
        if (DataContext is MainViewModel vm)
            await vm.LogPlaybackActionAsync("play", "Запущено воспроизведение видео");
    }

    private async void Pause_Click(object sender, RoutedEventArgs e)
    {
        VideoPlayer.MediaPlayerViewModel?.Pause();
        if (DataContext is MainViewModel vm)
        {
            await vm.LogPlaybackActionAsync("pause", "Видео поставлено на паузу");
            await vm.PauseAnalysisAsync();
        }
    }

    private async void Stop_Click(object sender, RoutedEventArgs e)
    {
        VideoPlayer.MediaPlayerViewModel?.Stop();
        _wasStopped = true;
        if (DataContext is MainViewModel vm)
        {
            await vm.LogPlaybackActionAsync("stop", "Воспроизведение видео остановлено");
            await vm.StopVideoAndAnalysisAsync();
        }
    }

    private async Task RestartStoppedPlayerAsync(MainViewModel vm)
    {
        if (!_wasStopped || string.IsNullOrWhiteSpace(vm.VideoPath))
            return;

        var path = vm.VideoPath;
        vm.VideoPath = string.Empty;
        await Task.Delay(50);
        vm.VideoPath = path;
        await Task.Delay(200);
        _wasStopped = false;
    }
}
