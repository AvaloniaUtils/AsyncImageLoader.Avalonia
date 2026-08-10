using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncImageLoader.Core;

/// <summary>
/// Owns an HTTP response for as long as its response stream is in use.
/// </summary>
internal sealed class HttpResponseStream : Stream {
    private readonly HttpResponseMessage _response;
    private Stream? _innerStream;

    public HttpResponseStream(Stream innerStream, HttpResponseMessage response) {
        _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        _response = response ?? throw new ArgumentNullException(nameof(response));
    }

    public override bool CanRead => _innerStream?.CanRead ?? false;
    public override bool CanSeek => _innerStream?.CanSeek ?? false;
    public override bool CanWrite => _innerStream?.CanWrite ?? false;
    public override long Length => InnerStream.Length;
    public override long Position {
        get => InnerStream.Position;
        set => InnerStream.Position = value;
    }

    public override void Flush() => InnerStream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) =>
        InnerStream.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) =>
        InnerStream.Read(buffer, offset, count);

    public override int Read(Span<byte> buffer) => InnerStream.Read(buffer);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        InnerStream.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default) =>
        InnerStream.ReadAsync(buffer, cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => InnerStream.Seek(offset, origin);

    public override void SetLength(long value) => InnerStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) =>
        InnerStream.Write(buffer, offset, count);

    public override void Write(ReadOnlySpan<byte> buffer) => InnerStream.Write(buffer);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        InnerStream.WriteAsync(buffer, offset, count, cancellationToken);

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default) =>
        InnerStream.WriteAsync(buffer, cancellationToken);

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _innerStream?.Dispose();
            _innerStream = null;
            _response.Dispose();
        }

        base.Dispose(disposing);
    }

    private Stream InnerStream =>
        _innerStream ?? throw new ObjectDisposedException(nameof(HttpResponseStream));
}
