using Avalonia.Controls.Shapes;
using Client.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Services
{
    public class StreamService
    {
        private readonly IApiClient _apiClient;
        private Task _streamReadingTask;
        private bool _isStreaming;
        public StreamService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }
        public async Task SendVideoAsync(string path, Action<StreamFrame> onFrameReceived)
        {
            await StopAsync();

            var response = await _apiClient.PostRawAsync("/stream/upload", path);
            var stream = await response.Content.ReadAsStreamAsync();
            _streamReadingTask = Task.Run(() => ReadStreamAsync(stream, onFrameReceived));
        }
        public async Task StartCameraAsync(string num, Action<StreamFrame> onFrameReceived)
        {
            var response = await _apiClient.PostRawAsync("/stream/start", num);
            var stream = await response.Content.ReadAsStreamAsync();
            _streamReadingTask = Task.Run(() => ReadStreamAsync(stream, onFrameReceived));
        }
        public async Task<HealthStatus?> GetStatusAsync()
        {
            return await _apiClient.GetAsync<HealthStatus>("/stream/status");
        }

        public async Task StopAsync()
        {
            
            if (_streamReadingTask != null)
            {
                try { await _streamReadingTask; } catch { }
                _isStreaming = false;
                _streamReadingTask = null;
            }
            await _apiClient.PostAsync<object>("/stream/stop", new { });
        }
        private async Task ReadStreamAsync(
            Stream stream,
            Action<StreamFrame> onFrameReceived)
        {
            using var reader = new StreamReader(stream);
            try
            {
                _isStreaming = true;
                while (_isStreaming)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;

                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var frame = JsonSerializer.Deserialize<StreamFrame>(line);
                        if (frame != null)
                            onFrameReceived(frame);
                    }
                    catch (JsonException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"JSON error: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ожидаемая отмена
                System.Diagnostics.Debug.WriteLine("Stream reading cancelled");
            }
            catch (Exception ex)
            {
            }
        }
    }
}