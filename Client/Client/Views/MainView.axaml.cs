using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Client.ViewModels;
using LibVLCSharp.Shared;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Client;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    private INotifyPropertyChanged? _boundViewModel;
    private bool _wasStopped;

    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private WriteableBitmap? _videoBitmap;
    private byte[]? _frameBuffer;
    private GCHandle _frameHandle;
    private int _vlcW, _vlcH;

    public MainView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BindViewModel();
        DetachedFromVisualTree += (_, _) =>
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVlc?.Dispose();
        };
        InitVlc();
    }

    private void InitVlc()
    {
        Core.Initialize();
        _libVlc = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVlc);
        _mediaPlayer.SetVideoFormatCallbacks(VideoFormatCallback, VideoCleanupCallback);
        _mediaPlayer.SetVideoCallbacks(LockCallback, null, DisplayCallback);
    }

    private uint VideoFormatCallback(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
    {
        Marshal.Copy(System.Text.Encoding.ASCII.GetBytes("BGRA"), 0, chroma, 4);
        _vlcW = (int)width;
        _vlcH = (int)height;

        if (_frameHandle.IsAllocated) _frameHandle.Free();
        _frameBuffer = new byte[_vlcW * _vlcH * 4];
        _frameHandle = GCHandle.Alloc(_frameBuffer, GCHandleType.Pinned);

        pitches = (uint)(_vlcW * 4);
        lines = (uint)_vlcH;

        int w = _vlcW, h = _vlcH;
        Dispatcher.UIThread.Post(() =>
        {
            _videoBitmap = new WriteableBitmap(
                new Avalonia.PixelSize(w, h),
                new Avalonia.Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);
            VideoImage.Source = _videoBitmap;
            if (DataContext is MainViewModel vm)
                vm.SetVideoSourceSize(w, h);
        });

        return 1;
    }

    private void VideoCleanupCallback(ref IntPtr opaque)
    {
        if (_frameHandle.IsAllocated) _frameHandle.Free();
    }

    private IntPtr LockCallback(IntPtr opaque, IntPtr planes)
    {
        if (_frameHandle.IsAllocated)
            Marshal.WriteIntPtr(planes, _frameHandle.AddrOfPinnedObject());
        return IntPtr.Zero;
    }

    private void DisplayCallback(IntPtr opaque, IntPtr picture)
    {
        var buf = _frameBuffer;
        var bmp = _videoBitmap;
        if (buf == null || bmp == null) return;
        var copy = new byte[buf.Length];
        Buffer.BlockCopy(buf, 0, copy, 0, copy.Length);
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                using var fb = bmp.Lock();
                Marshal.Copy(copy, 0, fb.Address, copy.Length);
            }
            catch { }
        }, DispatcherPriority.Render);
    }

    private void BindViewModel()
    {
        if (_boundViewModel != null)
            _boundViewModel.PropertyChanged -= ViewModelOnPropertyChanged;

        _boundViewModel = DataContext as INotifyPropertyChanged;
        if (_boundViewModel != null)
            _boundViewModel.PropertyChanged += ViewModelOnPropertyChanged;

        if (DataContext is MainViewModel vm)
        {
            if (VideoGrid.Bounds.Width > 0)
                vm.UpdateVideoViewport(VideoGrid.Bounds.Width, VideoGrid.Bounds.Height);
            if (!string.IsNullOrWhiteSpace(vm.VideoPath))
                PlayPath(vm.VideoPath);
        }

        RebuildOverlayCanvas();
    }

    private void RebuildOverlayCanvas()
    {
        OverlayCanvas.Children.Clear();
        if (DataContext is not MainViewModel vm) return;
        var red = new SolidColorBrush(Color.FromRgb(255, 50, 50));
        foreach (var d in vm.OverlayDetections)
        {
            var w = Math.Max(d.Width, 16);
            var h = Math.Max(d.Height, 16);
            var rect = new Rectangle
            {
                Width = w,
                Height = h,
                Stroke = red,
                StrokeThickness = 3,
                Fill = new SolidColorBrush(Color.FromArgb(80, 255, 50, 50))
            };
            Canvas.SetLeft(rect, d.Left - (w - d.Width) / 2);
            Canvas.SetTop(rect, d.Top - (h - d.Height) / 2);
            OverlayCanvas.Children.Add(rect);
        }
    }

    private async void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainViewModel vm) return;
        if (e.PropertyName == nameof(MainViewModel.OverlayDetections))
        {
            RebuildOverlayCanvas();
            return;
        }
        if (e.PropertyName != nameof(MainViewModel.VideoPath))
            return;

        if (string.IsNullOrWhiteSpace(vm.VideoPath))
        {
            _mediaPlayer?.Stop();
            VideoImage.Source = null;
            return;
        }

        _wasStopped = false;
        await Task.Delay(100);
        PlayPath(vm.VideoPath);
    }

    private void PlayPath(string path)
    {
        if (_mediaPlayer == null || _libVlc == null) return;
        _mediaPlayer.Stop();
        var media = new Media(_libVlc, path, FromType.FromPath);
        _mediaPlayer.Media = media;
        media.Dispose();
        _mediaPlayer.Play();
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel startVm)
        {
            await RestartStoppedPlayerAsync(startVm);
            await startVm.StartVideoAndAnalysisAsync();
        }

        _mediaPlayer?.Play();
        if (DataContext is MainViewModel vm)
            await vm.LogPlaybackActionAsync("play", "Запущено воспроизведение видео");
    }

    private async void Pause_Click(object sender, RoutedEventArgs e)
    {
        _mediaPlayer?.Pause();
        if (DataContext is MainViewModel vm)
        {
            await vm.LogPlaybackActionAsync("pause", "Видео поставлено на паузу");
            await vm.PauseAnalysisAsync();
        }
    }

    private async void Stop_Click(object sender, RoutedEventArgs e)
    {
        _mediaPlayer?.Stop();
        _wasStopped = true;
        if (DataContext is MainViewModel vm)
        {
            await vm.LogPlaybackActionAsync("stop", "Воспроизведение видео остановлено");
            await vm.StopVideoAndAnalysisAsync();
        }
    }

    private void VideoGrid_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.UpdateVideoViewport(e.NewSize.Width, e.NewSize.Height);
    }

    private async Task RestartStoppedPlayerAsync(MainViewModel vm)
    {
        if (!_wasStopped || string.IsNullOrWhiteSpace(vm.VideoPath))
            return;
        _mediaPlayer?.Stop();
        await Task.Delay(100);
        PlayPath(vm.VideoPath);
        await Task.Delay(100);
        _wasStopped = false;
    }
}
