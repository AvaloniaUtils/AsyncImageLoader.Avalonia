using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using Avalonia.Media.Imaging;
using AwesomeAssertions;
using Xunit;

namespace AsyncImageLoader.Avalonia.Tests;

public sealed class ImageLoaderPipelineTests {
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task MemoryCachePreventsSecondDecodeAndTransportCall() {
        var handlerCalls = 0;
        var decodeCalls = 0;
        using var client = new HttpClient(new TestHttpMessageHandler(_ => {
            Interlocked.Increment(ref handlerCalls);
            return TestHttpMessageHandler.CreateResponse(Png);
        }));
        using var cache = new MemoryImageCache();
        using var pipeline = new ImageLoaderPipeline(
            new CompositeImageSourceResolver(
                new FileImageSourceResolver(),
                new AvaloniaAssetSourceResolver()),
            new HttpImageTransport(client),
            new CountingDecoder(() => Interlocked.Increment(ref decodeCalls)),
            cache);

        using var first = await pipeline.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));
        using var second = await pipeline.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first.Image.Should().BeSameAs(second.Image);
        handlerCalls.Should().Be(1);
        decodeCalls.Should().Be(1);
    }

    [Fact]
    public async Task LocalSourceDoesNotUseHttpTransport() {
        var path = Path.GetTempFileName();
        try {
            await File.WriteAllBytesAsync(path, Png);
            using var client = new HttpClient(new TestHttpMessageHandler(_ =>
                throw new InvalidOperationException("HTTP must not be called.")));
            using var pipeline = new ImageLoaderPipeline(
                new CompositeImageSourceResolver(new FileImageSourceResolver()),
                new HttpImageTransport(client),
                new BitmapDecoder(),
                new MemoryImageCache());

            using var lease = await pipeline.LoadAsync(new ImageLoadRequest(path));

            lease.Should().NotBeNull();
            ((Bitmap)lease.Image).Size.Width.Should().Be(1);
        }
        finally {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task DiskCacheHitDoesNotUseHttpTransport() {
        var directory = Directory.CreateTempSubdirectory("pipeline-cache-").FullName;
        try {
            var byteCache = new DiskImageByteCache(directory);
            await using (var data = new MemoryStream(Png))
                await byteCache.SetAsync("https://example.test/image.png", data);

            using var client = new HttpClient(new TestHttpMessageHandler(_ =>
                throw new InvalidOperationException("HTTP must not be called.")));
            using var pipeline = new ImageLoaderPipeline(
                new CompositeImageSourceResolver(new FileImageSourceResolver()),
                new HttpImageTransport(client),
                new BitmapDecoder(),
                new MemoryImageCache(),
                byteCache);

            using var lease = await pipeline.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));

            lease.Should().NotBeNull();
        }
        finally {
            Directory.Delete(directory, true);
        }
    }

    private sealed class CountingDecoder : IBitmapDecoder {
        private readonly Action _onDecode;

        public CountingDecoder(Action onDecode) {
            _onDecode = onDecode;
        }

        public Task<Bitmap> DecodeAsync(Stream stream, CancellationToken cancellationToken = default) {
            _onDecode();
            return Task.FromResult(new Bitmap(stream));
        }
    }
}
