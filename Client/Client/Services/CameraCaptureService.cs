using Client.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Services
{
    public class CameraCaptureService : IDisposable
    {
        private CancellationTokenSource? _cts;
        private readonly IApiClient _apiClient;
        private readonly EventsService _eventsService;

        public CameraCaptureService(IApiClient apiClient, EventsService eventsService)
        {
            _apiClient = apiClient;
            _eventsService = eventsService;
        }

        public Task StartCaptureAsync(int cameraIndex = 0)
        {
            _cts = new CancellationTokenSource();
            return Task.CompletedTask;
        }

        public void StopCapture()
        {
            _cts?.Cancel();
        }

        public void Dispose()
        {
            StopCapture();
        }
    }
}
