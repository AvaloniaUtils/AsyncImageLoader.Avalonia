using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using AsyncImageLoader.Core.Pipeline;
using AsyncImageLoader.Loaders;
using Avalonia.Media.Imaging;
using AwesomeAssertions;
using Xunit;

namespace AsyncImageLoader.Avalonia.Tests;

public sealed class BaseWebImageLoaderTests {
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task LoadsBitmapFromHttpResponse() {
        using var client = new HttpClient(new TestHttpMessageHandler(request => {
            request.RequestUri.Should().Be(new Uri("https://example.test/image.png"));
            return TestHttpMessageHandler.CreateResponse(Png);
        }));
        using var loader = new BaseWebImageLoader(client, false);

        using var lease = await loader.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));
        var bitmap = lease?.Image as Bitmap;

        bitmap.Should().NotBeNull();
        bitmap.Size.Width.Should().Be(1);
        bitmap.Size.Height.Should().Be(1);
    }

    [Fact]
    public async Task LoadsBitmapFromNonSeekableHttpResponse() {
        using var client = new HttpClient(new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StreamContent(new NonSeekableReadStream(Png))
        }));
        using var loader = new BaseWebImageLoader(client, false);

        using var lease = await loader.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));

        lease.Should().NotBeNull();
        lease.Image.Should().BeOfType<Bitmap>();
    }

    [Fact]
    public async Task ReturnsNullForHttpError() {
        using var client = new HttpClient(new TestHttpMessageHandler(_ =>
            TestHttpMessageHandler.CreateResponse(Array.Empty<byte>(), HttpStatusCode.NotFound)));
        using var loader = new BaseWebImageLoader(client, false);

        using var lease = await loader.LoadAsync(new ImageLoadRequest("https://example.test/missing.png"));
        var bitmap = lease?.Image;

        bitmap.Should().BeNull();
    }

    [Fact]
    public async Task DoesNotRetainImagesBetweenRequests() {
        var requests = 0;
        using var client = new HttpClient(new TestHttpMessageHandler(_ => {
            requests++;
            return TestHttpMessageHandler.CreateResponse(Png);
        }));
        using var loader = new BaseWebImageLoader(client, false);

        using var first = await loader.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));
        using var second = await loader.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first.Image.Should().NotBeSameAs(second.Image);
        requests.Should().Be(2);
    }

    private sealed class NonSeekableReadStream : Stream {
        private readonly MemoryStream _inner;

        public NonSeekableReadStream(byte[] content) {
            _inner = new MemoryStream(content, writable: false);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => _inner.Read(buffer);
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) => _inner.ReadAsync(buffer, cancellationToken);
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing) {
            if (disposing)
                _inner.Dispose();
            base.Dispose(disposing);
        }
    }

}
