using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        public async Task StartMjpegAsync(Action<Bitmap> onFrame, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _apiClient.GetRawAsync("/stream/mjpeg");
                if (!response.IsSuccessStatusCode)
                    return;

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                while (!cancellationToken.IsCancellationRequested)
                {
                    int contentLength = 0;

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var line = await ReadByteLineAsync(stream, cancellationToken);
                        if (line == null) return;
                        if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                            contentLength = int.Parse(line.Split(':')[1].Trim());
                        else if (line.Length == 0 && contentLength > 0)
                            break;
                    }

                    if (contentLength <= 0) continue;

                    var buffer = new byte[contentLength];
                    int offset = 0;
                    while (offset < contentLength && !cancellationToken.IsCancellationRequested)
                    {
                        int n = await stream.ReadAsync(buffer.AsMemory(offset, contentLength - offset), cancellationToken);
                        if (n == 0) return;
                        offset += n;
                    }

                    try
                    {
                        var bitmap = new Bitmap(new MemoryStream(buffer));
                        onFrame(bitmap);
                    }
                    catch { }
                }
            }
            catch (OperationCanceledException) { }
            catch { }
        }

        private static async Task<string?> ReadByteLineAsync(Stream stream, CancellationToken ct)
        {
            var buf = new List<byte>(128);
            var tmp = new byte[1];
            while (true)
            {
                int n = await stream.ReadAsync(tmp, 0, 1, ct);
                if (n == 0) return null;
                if (tmp[0] == '\n')
                {
                    if (buf.Count > 0 && buf[^1] == '\r')
                        buf.RemoveAt(buf.Count - 1);
                    return Encoding.ASCII.GetString(buf.ToArray());
                }
                buf.Add(tmp[0]);
            }
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
