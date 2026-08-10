using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Loaders;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Headless;
using AwesomeAssertions;
using Xunit;

namespace AsyncImageLoader.Avalonia.Tests;

public sealed class RamCachedWebImageLoaderTests {
    static RamCachedWebImageLoaderTests() {
        AppBuilder.Configure<TestApplication>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
    }

    [Fact]
    public async Task RequestsForTheSameUrlAreDeduplicated() {
        using var loader = new TestLoader();

        var first = loader.ProvideImageAsync("image");
        var second = loader.ProvideImageAsync("image");
        var images = await Task.WhenAll(first, second);

        images[0].Should().BeSameAs(images[1]);
        loader.LoadCount.Should().Be(1);
    }

    [Fact]
    public async Task FailedLoadsAreNotCached() {
        using var loader = new TestLoader { ReturnNull = true };

        (await loader.ProvideImageAsync("image")).Should().BeNull();
        (await loader.ProvideImageAsync("image")).Should().BeNull();

        loader.LoadCount.Should().Be(2);
    }

    [Fact]
    public async Task AbsoluteExpirationKeepsLiveBitmapReusableAsWeakReference() {
        using var loader = new TestLoader(new RamCacheOptions {
            AbsoluteExpiration = TimeSpan.FromMilliseconds(150)
        });

        var first = await loader.ProvideImageAsync("image").WaitAsync(TimeSpan.FromSeconds(2));
        Thread.Sleep(350);
        var second = await loader.ProvideImageAsync("image").WaitAsync(TimeSpan.FromSeconds(2));

        second.Should().BeSameAs(first);
        loader.LoadCount.Should().Be(1);
    }

    [Fact]
    public async Task SlidingExpirationRenewsStrongRetentionOnAccess() {
        using var loader = new TestLoader(new RamCacheOptions {
            SlidingExpiration = TimeSpan.FromMilliseconds(150)
        });

        var first = await loader.ProvideImageAsync("image");
        Thread.Sleep(80);
        var second = await loader.ProvideImageAsync("image");
        Thread.Sleep(80);
        var third = await loader.ProvideImageAsync("image");

        second.Should().BeSameAs(first);
        third.Should().BeSameAs(first);
        loader.LoadCount.Should().Be(1);
    }

    [Fact]
    public async Task AbsoluteExpirationWinsOverSlidingExpiration() {
        using var loader = new TestLoader(new RamCacheOptions {
            AbsoluteExpiration = TimeSpan.FromMilliseconds(180),
            SlidingExpiration = TimeSpan.FromMilliseconds(500)
        });

        var first = await loader.ProvideImageAsync("image");
        Thread.Sleep(100);
        (await loader.ProvideImageAsync("image")).Should().BeSameAs(first);
        Thread.Sleep(180);
        (await loader.ProvideImageAsync("image")).Should().BeSameAs(first);

        loader.LoadCount.Should().Be(1);
    }

    [Fact]
    public async Task SlidingExpirationDoesNotRestoreStrongRetentionAfterAbsoluteExpiration() {
        using var loader = new TestLoader(new RamCacheOptions {
            AbsoluteExpiration = TimeSpan.FromMilliseconds(150),
            SlidingExpiration = TimeSpan.FromMilliseconds(40)
        });

        var first = await loader.ProvideImageAsync("image");
        Thread.Sleep(250);

        (await loader.ProvideImageAsync("image")).Should().BeSameAs(first);
        Thread.Sleep(100);
        (await loader.ProvideImageAsync("image")).Should().BeSameAs(first);

        loader.LoadCount.Should().Be(1);
    }

    [Fact]
    public void InvalidExpirationIsRejected() {
        var action = () => new RamCachedWebImageLoader(new RamCacheOptions {
            SlidingExpiration = TimeSpan.Zero
        });

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void HttpClientConstructorAcceptsCacheOptions() {
        using var loader = new RamCachedWebImageLoader(
            new HttpClient(),
            false,
            new RamCacheOptions { AbsoluteExpiration = TimeSpan.FromMinutes(1) });

        loader.Should().NotBeNull();
    }

    private sealed class TestLoader : RamCachedWebImageLoader {
        private static readonly byte[] Png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        public TestLoader(RamCacheOptions? options = null) : base(options) { }

        private int _loadCount;

        public int LoadCount => _loadCount;
        public bool ReturnNull { get; set; }

        protected override Task<Bitmap?> LoadAsync(string url) {
            Interlocked.Increment(ref _loadCount);
            if (ReturnNull)
                return Task.FromResult<Bitmap?>(null);

            using var stream = new MemoryStream(Png);
            return Task.FromResult<Bitmap?>(new Bitmap(stream));
        }
    }

    private sealed class TestApplication : Application { }
}
