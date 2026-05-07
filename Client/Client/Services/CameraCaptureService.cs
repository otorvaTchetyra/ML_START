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
        private int _frameCounter = 0;
        private readonly int _sendEveryNFrames = 10;
        private readonly IApiClient _apiClient;
        private readonly EventsService _eventsService;

        public event Action<Mat> OnFrameCaptured;
        public event Action<byte[], FeedingEvent> OnDetectionResult;

        public CameraCaptureService(IApiClient apiClient, EventsService eventsService)
        {
            _apiClient = apiClient;
            _eventsService = eventsService;
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

                _frameCounter++;
                OnFrameCaptured?.Invoke(frame.Clone());

                // Отправляем каждый 10-й кадр
                if (_frameCounter % _sendEveryNFrames == 0)
                {
                    await SendFrameForDetection(frame);
                }

                await Task.Delay(33); // ~30 FPS
            }
        }

        private async Task SendFrameForDetection(Mat frame)
        {
            // Конвертируем Mat в byte[]
            var imageBytes = frame.ToBytes(".jpg");

            // Отправляем на сервер
            //var result = await _apiClient.PostAsync<FeedingEvent>("api/detect", imageBytes);

            // Сохраняем событие
            /*var feedingEvent = new FeedingEvent
            {
                Timestamp = DateTime.Now,
                GranuleCount = result.GranuleCount,
                IntensityPerSec = result.IntensityPerSec,
                IsOutOfSchedule = IsFeedingTimeOutOfSchedule()
            };*/

            //await _eventsService.AddEventAsync(feedingEvent);
            //OnDetectionResult?.Invoke(imageBytes, feedingEvent);
        }

        private bool IsFeedingTimeOutOfSchedule()
        {
            // Проверка расписания кормления
            //var currentTime = DateTime.Now.TimeOfDay;
            //var schedule = _scheduleService.GetCurrentSchedule();
            //return !schedule.Contains(currentTime);
            return false;
        }

        public void Dispose()
        {
            StopCapture();
            _capture?.Dispose();
        }
    }
}
