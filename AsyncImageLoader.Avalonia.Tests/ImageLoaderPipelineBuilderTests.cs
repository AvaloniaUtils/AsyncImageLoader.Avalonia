using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core.Decoding;
using AsyncImageLoader.Core.Pipeline;
using AsyncImageLoader.Core.Sources;
using AsyncImageLoader.Core.Transport;
using Avalonia.Media.Imaging;
using AwesomeAssertions;
using Xunit;

namespace AsyncImageLoader.Avalonia.Tests;

public sealed class ImageLoaderPipelineBuilderTests {
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task PresetComponentsCanBeReplaced() {
        var transport = new TestTransport();
        var decoder = new TestDecoder();
        using var pipeline = ImageLoaderPipelineBuilder.RamCached()
            .UseSourceResolver(new NullResolver())
            .UseTransport(transport)
            .UseDecoder(decoder)
            .Build();

        using var lease = await pipeline.LoadAsync(new ImageLoadRequest("custom://image"));

        lease.Should().NotBeNull();
        transport.Requests.Should().Be(1);
        decoder.Decodes.Should().Be(1);
    }

    [Fact]
    public void BuilderCanOnlyTransferOwnedComponentsOnce() {
        var builder = ImageLoaderPipelineBuilder.Uncached();
        using var pipeline = builder.Build();

        var action = () => builder.Build();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task PipelineDisposesOwnedHttpClient() {
        var client = new HttpClient(new TestHttpMessageHandler(_ => TestHttpMessageHandler.CreateResponse(Png)));
        var pipeline = ImageLoaderPipelineBuilder.Uncached()
            .UseHttpClient(client, disposeHttpClient: true)
            .Build();

        pipeline.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.GetAsync("https://example.test/image.png"));
    }

    private sealed class NullResolver : IImageSourceResolver {
        public Task<ResolvedImageSource?> ResolveAsync(
            ImageLoadRequest request,
            CancellationToken cancellationToken = default) {
            return Task.FromResult<ResolvedImageSource?>(null);
        }
    }

    private sealed class TestTransport : IImageTransport {
        public int Requests { get; private set; }

        public Task<Stream?> GetAsync(
            ImageLoadRequest request,
            CancellationToken cancellationToken = default) {
            Requests++;
            return Task.FromResult<Stream?>(new MemoryStream(Png, writable: false));
        }
    }

    private sealed class TestDecoder : IBitmapDecoder {
        public int Decodes { get; private set; }

        public Task<Bitmap> DecodeAsync(Stream stream, CancellationToken cancellationToken = default) {
            Decodes++;
            return Task.FromResult(new Bitmap(stream));
        }
    }
}
