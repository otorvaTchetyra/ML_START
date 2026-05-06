using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Client.ViewModels;


namespace Client;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
    }
    private void Play_Click(object sender, RoutedEventArgs e) => VideoPlayer.MediaPlayerViewModel?.Play();
    private void Pause_Click(object sender, RoutedEventArgs e) => VideoPlayer.MediaPlayerViewModel?.Pause();
    private void Stop_Click(object sender, RoutedEventArgs e) => VideoPlayer.MediaPlayerViewModel?.Stop();
}