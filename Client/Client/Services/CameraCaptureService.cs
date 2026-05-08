using Client.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Services
{
    public class CameraCaptureService : IDisposable
    {
        private VideoCapture _capture;
        private CancellationTokenSource _cts;
        private readonly IApiClient _apiClient;
        private readonly EventsService _eventsService;
        private readonly StreamService _streamService;

        public event Action<Mat> OnFrameCaptured;
        public event Action<byte[], FeedingEvent> OnDetectionResult;

        public CameraCaptureService(IApiClient apiClient, EventsService eventsService, StreamService streamService)
        {
            _apiClient = apiClient;
            _eventsService = eventsService;
            _streamService = streamService;
        }

        public async Task StartCaptureAsync(int cameraIndex = 0)
        {
            _cts = new CancellationTokenSource();
            _capture = new VideoCapture(cameraIndex);

            if (!_capture.IsOpened())
                throw new Exception("Не удалось открыть камеру");

            await Task.Run(() => ProcessFrames(_cts.Token));
        }

        public void StopCapture()
        {
            _cts?.Cancel();
            _capture?.Release();
        }

        private async void ProcessFrames(CancellationToken token)
        {
            using var frame = new Mat();

            while (!token.IsCancellationRequested && _capture.Read(frame))
            {
                if (frame.Empty()) break;

                OnFrameCaptured?.Invoke(frame.Clone());

                await Task.Delay(33); // ~30 FPS
            }
        }

        public void Dispose()
        {
            StopCapture();
            _capture?.Dispose();
        }
    }
}
