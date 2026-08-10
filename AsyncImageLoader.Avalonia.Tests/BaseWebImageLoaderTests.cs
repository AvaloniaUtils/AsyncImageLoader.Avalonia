using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AsyncImageLoader.Loaders;
using AsyncImageLoader.Core;
using AwesomeAssertions;
using Avalonia.Media.Imaging;
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
        bitmap!.Size.Width.Should().Be(1);
        bitmap.Size.Height.Should().Be(1);
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

}
