using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Client.ViewModels;
using LibVLCSharp.Shared;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Client;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    private INotifyPropertyChanged? _boundViewModel;
    private bool _wasStopped;

    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private WriteableBitmap? _videoBitmap;
    private IntPtr _unmanagedBuffer = IntPtr.Zero;
    private int _unmanagedBufferSize;
    private byte[]? _managedBuffer;
    private int _vlcW, _vlcH;
    private readonly object _frameLock = new();
    private volatile bool _ignoreEndReached;
    private long _lastFrameTicks;
    private const long FrameIntervalTicks = TimeSpan.TicksPerMillisecond * 33;

    public MainView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BindViewModel();
        InitVlc();
    }

    private void InitVlc()
    {
        Core.Initialize();
        _libVlc = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVlc);
        _mediaPlayer.SetVideoFormatCallbacks(VideoFormatCallback, VideoCleanupCallback);
        _mediaPlayer.SetVideoCallbacks(LockCallback, null, DisplayCallback);
        _mediaPlayer.EndReached += OnVideoEndReached;
    }

    private void OnVideoEndReached(object? sender, EventArgs e)
    {
        if (_ignoreEndReached) return;
        _wasStopped = true;
        Dispatcher.UIThread.Post(async () =>
        {
            VideoImage.Source = null;
            if (DataContext is MainViewModel vm)
                await vm.StopVideoAndAnalysisAsync(videoEnded: true);
        });
    }

    private uint VideoFormatCallback(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
    {
        Marshal.Copy(System.Text.Encoding.ASCII.GetBytes("BGRA"), 0, chroma, 4);
        _vlcW = (int)width;
        _vlcH = (int)height;
        int size = _vlcW * _vlcH * 4;

        lock (_frameLock)
        {
            if (_unmanagedBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal(_unmanagedBuffer);
            _unmanagedBuffer = Marshal.AllocHGlobal(size);
            _unmanagedBufferSize = size;
            _managedBuffer = new byte[size];
        }

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
        lock (_frameLock)
        {
            if (_unmanagedBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_unmanagedBuffer);
                _unmanagedBuffer = IntPtr.Zero;
                _unmanagedBufferSize = 0;
                _managedBuffer = null;
            }
        }
    }

    private IntPtr LockCallback(IntPtr opaque, IntPtr planes)
    {
        lock (_frameLock)
        {
            if (_unmanagedBuffer != IntPtr.Zero)
                Marshal.WriteIntPtr(planes, _unmanagedBuffer);
        }
        return IntPtr.Zero;
    }

    private void DisplayCallback(IntPtr opaque, IntPtr picture)
    {
        var now = DateTime.UtcNow.Ticks;
        if (now - Interlocked.Read(ref _lastFrameTicks) < FrameIntervalTicks) return;
        Interlocked.Exchange(ref _lastFrameTicks, now);

        byte[]? buf;
        int len;
        lock (_frameLock)
        {
            if (_unmanagedBuffer == IntPtr.Zero || _managedBuffer == null) return;
            buf = _managedBuffer;
            len = buf.Length;
            Marshal.Copy(_unmanagedBuffer, buf, 0, len);
        }
        Dispatcher.UIThread.Post(() =>
        {
            var bmp = _videoBitmap;
            if (bmp == null) return;
            try
            {
                using var fb = bmp.Lock();
                Marshal.Copy(buf, 0, fb.Address, len);
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
            if (!string.IsNullOrWhiteSpace(vm.VideoPath) && _mediaPlayer?.IsPlaying != true)
                _ = PlayPath(vm.VideoPath);
        }

        UpdateOverlay();
    }

    private void UpdateOverlay()
    {
        if (DataContext is MainViewModel vm)
            OverlayControl.Update(vm.OverlayDetections);
        else
            OverlayControl.Update(Array.Empty<Client.Models.OverlayDetection>());
    }

    private async void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainViewModel vm) return;
        if (e.PropertyName == nameof(MainViewModel.OverlayDetections))
        {
            UpdateOverlay();
            return;
        }
        if (e.PropertyName != nameof(MainViewModel.VideoPath))
            return;

        if (string.IsNullOrWhiteSpace(vm.VideoPath))
        {
            _ignoreEndReached = true;
            _mediaPlayer?.Stop();
            VideoImage.Source = null;
            return;
        }

        _wasStopped = false;
        await Task.Delay(100);
        await PlayPath(vm.VideoPath);
    }

    private async Task PlayPath(string path)
    {
        if (_mediaPlayer == null || _libVlc == null) return;
        _ignoreEndReached = true;
        _mediaPlayer.Stop();
        await Task.Delay(300);
        var media = new Media(_libVlc, path, FromType.FromPath);
        _mediaPlayer.Media = media;
        media.Dispose();
        _ignoreEndReached = false;
        _mediaPlayer.Play();
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel startVm)
        {
            bool restarted = await RestartStoppedPlayerAsync(startVm);
            await startVm.StartVideoAndAnalysisAsync();
            if (!restarted)
                _mediaPlayer?.Play();
        }
        else
        {
            _mediaPlayer?.Play();
        }
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
        _ignoreEndReached = true;
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

    private async Task<bool> RestartStoppedPlayerAsync(MainViewModel vm)
    {
        if (!_wasStopped || string.IsNullOrWhiteSpace(vm.VideoPath))
            return false;
        await PlayPath(vm.VideoPath);
        _wasStopped = false;
        return true;
    }
}
