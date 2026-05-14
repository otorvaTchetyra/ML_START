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
        private Task? _streamReadingTask;
        private CancellationTokenSource? _streamCancellation;
        private Stream? _activeStream;
        private HttpResponseMessage? _activeResponse;
        private bool _isStreaming;

        public StreamService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task SendVideoAsync(string path, Action<StreamFrame> onFrameReceived)
        {
            await StopAsync();

            var response = await _apiClient.PostFileAsync("/stream/upload", path);
            _activeResponse = response;
            var stream = await response.Content.ReadAsStreamAsync();
            _activeStream = stream;
            _streamCancellation = new CancellationTokenSource();
            _streamReadingTask = Task.Run(() => ReadStreamAsync(stream, onFrameReceived, _streamCancellation.Token));
        }

        public async Task StartCameraAsync(string num, Action<StreamFrame> onFrameReceived)
        {
            await StopAsync();

            var response = await _apiClient.PostRawAsync("/stream/start", new { source = num });
            _activeResponse = response;
            var stream = await response.Content.ReadAsStreamAsync();
            _activeStream = stream;
            _streamCancellation = new CancellationTokenSource();
            _streamReadingTask = Task.Run(() => ReadStreamAsync(stream, onFrameReceived, _streamCancellation.Token));
        }

        public async Task<HealthStatus?> GetStatusAsync()
        {
            return await _apiClient.GetAsync<HealthStatus>("/stream/status");
        }

        public async Task StopAsync()
        {
            _isStreaming = false;
            _streamCancellation?.Cancel();
            _activeStream?.Dispose();
            _activeResponse?.Dispose();

            try
            {
                await _apiClient.PostAsync<object>("/stream/stop", new { });
            }
            catch
            {
            }

            if (_streamReadingTask != null)
            {
                try { await Task.WhenAny(_streamReadingTask, Task.Delay(500)); } catch { }
                _streamReadingTask = null;
            }

            _streamCancellation?.Dispose();
            _streamCancellation = null;
            _activeStream = null;
            _activeResponse = null;
        }

        private async Task ReadStreamAsync(Stream stream, Action<StreamFrame> onFrameReceived, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(stream);
            try
            {
                _isStreaming = true;
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                while (_isStreaming && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null)
                        break;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        var frame = JsonSerializer.Deserialize<StreamFrame>(line, jsonOptions);
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
                System.Diagnostics.Debug.WriteLine("Stream reading cancelled");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }
}
