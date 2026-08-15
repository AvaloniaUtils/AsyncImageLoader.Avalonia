using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using AsyncImageLoader.Loaders;
using AwesomeAssertions;
using Xunit;

namespace AsyncImageLoader.Avalonia.Tests;

public sealed class RamCachedWebImageLoaderTests {
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task RequestsForTheSameUrlAreDeduplicated() {
        var response = new TaskCompletionSource<HttpResponseMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var requestStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = 0;
        using var client = new HttpClient(new AsyncHttpMessageHandler(async (_, cancellationToken) => {
            if (Interlocked.Increment(ref requests) == 1)
                requestStarted.SetResult(true);
            return await response.Task.WaitAsync(cancellationToken);
        }));
        using var loader = new RamCachedWebImageLoader(client, false);

        var firstTask = loader.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));
        await requestStarted.Task;
        var secondTask = loader.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));
        response.SetResult(TestHttpMessageHandler.CreateResponse(Png));
        using var first = await firstTask;
        using var second = await secondTask;

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first.Image.Should().BeSameAs(second.Image);
        requests.Should().Be(1);
    }

    [Fact]
    public async Task FailedLoadsAreNotCached() {
        var requests = 0;
        using var client = new HttpClient(new TestHttpMessageHandler(_ => {
            Interlocked.Increment(ref requests);
            return TestHttpMessageHandler.CreateResponse(Array.Empty<byte>(), HttpStatusCode.NotFound);
        }));
        using var loader = new RamCachedWebImageLoader(client, false);

        (await loader.LoadAsync(new ImageLoadRequest("https://example.test/missing.png"))).Should().BeNull();
        (await loader.LoadAsync(new ImageLoadRequest("https://example.test/missing.png"))).Should().BeNull();

        requests.Should().Be(2);
    }

    [Fact]
    public async Task ExpiredImageRemainsReusableWhileLeaseIsActive() {
        var requests = 0;
        using var client = new HttpClient(new TestHttpMessageHandler(_ => {
            Interlocked.Increment(ref requests);
            return TestHttpMessageHandler.CreateResponse(Png);
        }));
        var timeProvider = new TestTimeProvider();
        using var loader = new RamCachedWebImageLoader(client, false, new MemoryImageCacheOptions {
            AbsoluteExpiration = TimeSpan.FromMilliseconds(100)
        }, timeProvider);
        using var first = await loader.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));

        timeProvider.Advance(TimeSpan.FromMilliseconds(250));
        using var second = await loader.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        second.Image.Should().BeSameAs(first.Image);
        requests.Should().Be(1);
    }

    [Fact]
    public async Task ExpiredImageIsReloadedAfterLastLeaseIsReleased() {
        var requests = 0;
        using var client = new HttpClient(new TestHttpMessageHandler(_ => {
            Interlocked.Increment(ref requests);
            return TestHttpMessageHandler.CreateResponse(Png);
        }));
        var timeProvider = new TestTimeProvider();
        using var loader = new RamCachedWebImageLoader(client, false, new MemoryImageCacheOptions {
            AbsoluteExpiration = TimeSpan.FromMilliseconds(100)
        }, timeProvider);
        var first = await loader.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));
        first!.Dispose();

        timeProvider.Advance(TimeSpan.FromMilliseconds(250));
        using var second = await loader.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));

        second.Should().NotBeNull();
        requests.Should().Be(2);
    }

    [Fact]
    public async Task ClearRamCacheReloadsImageWithoutInvalidatingActiveLease() {
        var requests = 0;
        using var client = new HttpClient(new TestHttpMessageHandler(_ => {
            Interlocked.Increment(ref requests);
            return TestHttpMessageHandler.CreateResponse(Png);
        }));
        using var loader = new RamCachedWebImageLoader(client, false);
        using var first = await loader.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));

        loader.ClearRamCache();
        first!.Image.Size.Width.Should().Be(1);
        using var second = await loader.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));

        second.Should().NotBeNull();
        second.Image.Should().NotBeSameAs(first.Image);
        requests.Should().Be(2);
    }

    [Fact]
    public void InvalidExpirationIsRejected() {
        var action = () => new RamCachedWebImageLoader(new MemoryImageCacheOptions {
            SlidingExpiration = TimeSpan.Zero
        });

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    private sealed class AsyncHttpMessageHandler : HttpMessageHandler {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public AsyncHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            return _handler(request, cancellationToken);
        }
    }
}
