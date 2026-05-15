using System;
using System.IO;

namespace Client.Services;

internal sealed class ProgressStream : Stream
{
    private readonly Stream _inner;
    private readonly long _totalBytes;
    private long _bytesRead;
    private readonly IProgress<double> _progress;

    public ProgressStream(Stream inner, long totalBytes, IProgress<double> progress)
    {
        _inner = inner;
        _totalBytes = totalBytes;
        _progress = progress;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _inner.Length;
    public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytes = _inner.Read(buffer, offset, count);
        Report(bytes);
        return bytes;
    }

    public override async System.Threading.Tasks.ValueTask<int> ReadAsync(
        Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default)
    {
        var bytes = await _inner.ReadAsync(buffer, cancellationToken);
        Report(bytes);
        return bytes;
    }

    private void Report(int bytes)
    {
        if (bytes <= 0 || _totalBytes <= 0) return;
        _bytesRead += bytes;
        _progress.Report(Math.Min(100.0, (double)_bytesRead / _totalBytes * 100.0));
    }

    public override void Flush() => _inner.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();
        base.Dispose(disposing);
    }
}
