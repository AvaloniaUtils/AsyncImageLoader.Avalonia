using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core.Caching;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AsyncImageLoader.Avalonia.Tests;

public sealed class MemoryImageCacheTests {
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task ConcurrentCallersShareOneImageAndReceiveIndependentLeases() {
        using var cache = new MemoryImageCache();
        var loads = 0;
        var first = cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => Interlocked.Increment(ref loads)));
        var second = cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => Interlocked.Increment(ref loads)));

        using var firstLease = await first;
        using var secondLease = await second;

        firstLease.Should().NotBeNull();
        secondLease.Should().NotBeNull();
        firstLease.Image.Should().BeSameAs(secondLease.Image);
        loads.Should().Be(1);
    }

    [Fact]
    public async Task ClearDoesNotDisposeAnImageWithAnActiveLease() {
        using var cache = new MemoryImageCache();
        var lease = await cache.GetOrCreateAsync("image", _ => CreateImageAsync());
        var bitmap = (Bitmap)lease!.Image;

        cache.Clear();
        bitmap.Size.Width.Should().Be(1);

        lease.Dispose();
        var action = () => bitmap.Size;
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task ClearDetachesActiveEntryAndNextRequestLoadsAgain() {
        using var cache = new MemoryImageCache();
        var loads = 0;
        using var first = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));

        cache.Clear();
        using var second = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));

        second!.Image.Should().NotBeSameAs(first!.Image);
        loads.Should().Be(2);
    }

    [Fact]
    public async Task ReleasingOneOfTwoLeasesKeepsTheImageUsable() {
        using var cache = new MemoryImageCache();
        using var first = await cache.GetOrCreateAsync("image", _ => CreateImageAsync());
        using var second = await cache.GetOrCreateAsync("image", _ => CreateImageAsync());
        var bitmap = (Bitmap)second!.Image;

        // ReSharper disable once DisposeOnUsingVariable
        first!.Dispose();
        cache.Clear();

        bitmap.Size.Width.Should().Be(1);
    }

    [Fact]
    public async Task FailedLoadIsNotRetained() {
        using var cache = new MemoryImageCache();
        var loads = 0;

        (await cache.GetOrCreateAsync("image", _ => Task.FromResult<IImage?>(null))).Should().BeNull();
        (await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads))).Should().NotBeNull();

        loads.Should().Be(1);
    }

    [Fact]
    public async Task DisposedCacheRejectsNewLoadsButKeepsActiveLeaseUsable() {
        var cache = new MemoryImageCache();
        using var lease = await cache.GetOrCreateAsync("image", _ => CreateImageAsync());
        var bitmap = (Bitmap)lease!.Image;

        cache.Dispose();

        bitmap.Size.Width.Should().Be(1);
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            cache.GetOrCreateAsync("other", _ => CreateImageAsync()));
    }

    [Fact]
    public async Task LeaseIsIdempotentAndCannotBeUsedAfterDispose() {
        using var cache = new MemoryImageCache();
        var lease = await cache.GetOrCreateAsync("image", _ => CreateImageAsync());

        lease.Should().NotBeNull();
        lease.Dispose();
        lease.Dispose();

        Assert.Throws<ObjectDisposedException>(() => lease.Image);
    }

    [Fact]
    public async Task FaultedLoadIsNotRetained() {
        using var cache = new MemoryImageCache();
        var loads = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetOrCreateAsync(
            "image",
            _ => Task.FromException<IImage?>(new InvalidOperationException())));

        using var lease = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));

        lease.Should().NotBeNull();
        loads.Should().Be(1);
    }

    [Fact]
    public async Task SynchronouslyThrownFactoryExceptionAllowsRetry() {
        using var cache = new MemoryImageCache();

        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetOrCreateAsync(
            "image",
            _ => throw new InvalidOperationException()));

        using var retry = await cache.GetOrCreateAsync("image", _ => CreateImageAsync());

        retry.Should().NotBeNull();
    }

    [Fact]
    public async Task CancellingOneWaiterDoesNotCancelSharedLoad() {
        using var cache = new MemoryImageCache();
        using var cancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource<IImage?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var loads = 0;

        var cancelledWaiter = cache.GetOrCreateAsync("image", _ => {
            Interlocked.Increment(ref loads);
            return completion.Task;
        }, cancellation.Token);
        var activeWaiter = cache.GetOrCreateAsync("image", _ => {
            Interlocked.Increment(ref loads);
            return completion.Task;
        });

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledWaiter);
        completion.SetResult(CreateBitmap());
        using var lease = await activeWaiter;

        lease.Should().NotBeNull();
        loads.Should().Be(1);
    }

    [Fact]
    public async Task DisposingCacheDuringLoadDisposesCompletedImageAndReturnsNoLease() {
        using var completion = new ManualResetEventSlim();
        var imageCreated = new TaskCompletionSource<Bitmap>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cache = new MemoryImageCache();
        var load = cache.GetOrCreateAsync("image", _ => Task.Run<IImage?>(() => {
            // ReSharper disable once AccessToDisposedClosure
            completion.Wait();
            var image = CreateBitmap();
            imageCreated.SetResult(image);
            return image;
        }));

        cache.Dispose();
        completion.Set();
        var lease = await load;
        var image = await imageCreated.Task;

        lease.Should().BeNull();
        Assert.Throws<ObjectDisposedException>(() => image.Size);
    }

    [Fact]
    public async Task ClearDetachesAnInFlightEntryAndNextRequestStartsAnotherLoad() {
        using var completion = new ManualResetEventSlim();
        using var cache = new MemoryImageCache();
        var loads = 0;
        var load = cache.GetOrCreateAsync("image", _ => Task.Run<IImage?>(() => {
            Interlocked.Increment(ref loads);
            // ReSharper disable once AccessToDisposedClosure
            completion.Wait();
            return CreateBitmap();
        }));

        cache.Clear();
        completion.Set();
        using var first = await load;
        using var second = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first.Image.Should().NotBeSameAs(second.Image);
        loads.Should().Be(2);
    }

    [Fact]
    public async Task ExpiredImageRemainsSharedWhileItHasAnActiveLease() {
        var timeProvider = new FakeTimeProvider();
        using var cache = new MemoryImageCache(new MemoryImageCacheOptions {
            AbsoluteExpiration = TimeSpan.FromMilliseconds(100)
        }, timeProvider);
        var loads = 0;
        using var first = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));
        timeProvider.Advance(TimeSpan.FromMilliseconds(250));
        using var second = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));

        second!.Image.Should().BeSameAs(first!.Image);
        loads.Should().Be(1);
    }

    [Fact]
    public async Task ExpiredUnleasedImageIsRemovedAndReloaded() {
        var timeProvider = new FakeTimeProvider();
        using var cache = new MemoryImageCache(new MemoryImageCacheOptions {
            AbsoluteExpiration = TimeSpan.FromMilliseconds(100)
        }, timeProvider);
        var loads = 0;
        var first = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));
        first!.Dispose();
        timeProvider.Advance(TimeSpan.FromMilliseconds(250));
        using var second = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));

        second.Should().NotBeNull();
        loads.Should().Be(2);
    }

    [Fact]
    public async Task SlidingExpirationIsRenewedByAccess() {
        var timeProvider = new FakeTimeProvider();
        using var cache = new MemoryImageCache(new MemoryImageCacheOptions {
            SlidingExpiration = TimeSpan.FromMilliseconds(150)
        }, timeProvider);
        var loads = 0;
        using var first = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));
        timeProvider.Advance(TimeSpan.FromMilliseconds(80));
        using var second = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));
        timeProvider.Advance(TimeSpan.FromMilliseconds(80));
        using var third = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));

        first!.Image.Should().BeSameAs(second!.Image);
        second.Image.Should().BeSameAs(third!.Image);
        loads.Should().Be(1);
    }

    [Fact]
    public async Task AbsoluteExpirationWinsOverSlidingExpiration() {
        var timeProvider = new FakeTimeProvider();
        using var cache = new MemoryImageCache(new MemoryImageCacheOptions {
            AbsoluteExpiration = TimeSpan.FromMilliseconds(180),
            SlidingExpiration = TimeSpan.FromMilliseconds(500)
        }, timeProvider);
        var loads = 0;
        using var first = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));
        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        using var second = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));
        timeProvider.Advance(TimeSpan.FromMilliseconds(180));
        using var third = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));

        first!.Image.Should().BeSameAs(second!.Image);
        second.Image.Should().BeSameAs(third!.Image);
        loads.Should().Be(1);
    }

    [Fact]
    public async Task CleanupTimerRemovesExpiredUnleasedEntry() {
        var timeProvider = new FakeTimeProvider();
        using var cache = new MemoryImageCache(new MemoryImageCacheOptions {
            AbsoluteExpiration = TimeSpan.FromMilliseconds(100)
        }, timeProvider);
        var loads = 0;
        var first = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));
        first!.Dispose();

        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        using var second = await cache.GetOrCreateAsync("image", _ => CreateImageAsync(() => ++loads));

        second.Should().NotBeNull();
        loads.Should().Be(2);
    }

    [Fact]
    public void InvalidExpirationIsRejected() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryImageCache(new MemoryImageCacheOptions {
            AbsoluteExpiration = TimeSpan.Zero
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryImageCache(new MemoryImageCacheOptions {
            SlidingExpiration = TimeSpan.Zero
        }));
    }

    [Fact]
    public async Task InvalidArgumentsAreRejected() {
        using var cache = new MemoryImageCache();
        await Assert.ThrowsAsync<ArgumentException>(() => cache.GetOrCreateAsync("", _ => CreateImageAsync()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.GetOrCreateAsync("image", null!));
    }

    private static Task<IImage?> CreateImageAsync(Action? onCreate = null) {
        onCreate?.Invoke();
        return Task.FromResult<IImage?>(CreateBitmap());
    }

    private static Bitmap CreateBitmap() {
        using var stream = new MemoryStream(Png);
        return new Bitmap(stream);
    }
}
