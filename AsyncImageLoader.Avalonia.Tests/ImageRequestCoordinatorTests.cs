using System;
using AsyncImageLoader.Core;
using Avalonia;
using Avalonia.Media;
using AwesomeAssertions;
using Xunit;

namespace AsyncImageLoader.Avalonia.Tests;

public sealed class ImageRequestCoordinatorTests {
    [Fact]
    public void BeginningRequestCancelsPreviousRequest() {
        using var coordinator = new ImageRequestCoordinator();
        var first = coordinator.Begin();

        var second = coordinator.Begin();

        first.CancellationToken.IsCancellationRequested.Should().BeTrue();
        second.CancellationToken.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void StaleLeaseIsRejectedAndReleased() {
        using var coordinator = new ImageRequestCoordinator();
        var first = coordinator.Begin();
        coordinator.Begin();
        var image = new TestImage();

        var accepted = coordinator.TrySetLease(first, ImageLease.Owned(image));

        accepted.Should().BeFalse();
        image.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void BeginningRequestReleasesCurrentLease() {
        using var coordinator = new ImageRequestCoordinator();
        var first = coordinator.Begin();
        var image = new TestImage();
        coordinator.TrySetLease(first, ImageLease.Owned(image)).Should().BeTrue();

        coordinator.Begin();

        image.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void OnlyCurrentRequestCanComplete() {
        using var coordinator = new ImageRequestCoordinator();
        var first = coordinator.Begin();
        var second = coordinator.Begin();

        coordinator.TryComplete(first).Should().BeFalse();
        coordinator.TryComplete(second).Should().BeTrue();
        coordinator.TryComplete(second).Should().BeFalse();
    }

    [Fact]
    public void CompletedRequestCannotReplaceItsLease() {
        using var coordinator = new ImageRequestCoordinator();
        var request = coordinator.Begin();
        coordinator.TryComplete(request).Should().BeTrue();
        var image = new TestImage();

        coordinator.TrySetLease(request, ImageLease.Owned(image)).Should().BeFalse();

        image.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void LeaseReleaseCanReenterCoordinator() {
        using var coordinator = new ImageRequestCoordinator();
        var first = coordinator.Begin();
        ImageRequestCoordinator.Request reentrantRequest = default;
        var image = new TestImage();
        // ReSharper disable once AccessToDisposedClosure
        coordinator.TrySetLease(first, ImageLease.Create(image, () => reentrantRequest = coordinator.Begin()));

        var outerRequest = coordinator.Begin();

        reentrantRequest.CancellationToken.CanBeCanceled.Should().BeTrue();
        outerRequest.CancellationToken.IsCancellationRequested.Should().BeTrue();
        coordinator.TryComplete(reentrantRequest).Should().BeTrue();
        image.Dispose();
    }

    [Fact]
    public void ThrowingCancellationCallbackDoesNotPreventLeaseRelease() {
        using var coordinator = new ImageRequestCoordinator();
        var request = coordinator.Begin();
        request.CancellationToken.Register(static () => throw new InvalidOperationException());
        var image = new TestImage();
        coordinator.TrySetLease(request, ImageLease.Owned(image));

        coordinator.Cancel();

        image.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void ThrowingLeaseCallbackDoesNotBreakNextRequest() {
        using var coordinator = new ImageRequestCoordinator();
        var first = coordinator.Begin();
        coordinator.TrySetLease(first, ImageLease.Create(new TestImage(), static () =>
            throw new InvalidOperationException()));

        var second = coordinator.Begin();

        second.CancellationToken.IsCancellationRequested.Should().BeFalse();
        coordinator.TryComplete(second).Should().BeTrue();
    }

    [Fact]
    public void CancelReleasesLeaseAndInvalidatesRequest() {
        using var coordinator = new ImageRequestCoordinator();
        var request = coordinator.Begin();
        var image = new TestImage();
        coordinator.TrySetLease(request, ImageLease.Owned(image));

        coordinator.Cancel();

        request.CancellationToken.IsCancellationRequested.Should().BeTrue();
        coordinator.TryComplete(request).Should().BeFalse();
        image.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void RequestCanRestartAfterDetachCancellation() {
        using var coordinator = new ImageRequestCoordinator();
        var detachedRequest = coordinator.Begin();
        coordinator.Cancel();

        var attachedRequest = coordinator.Begin();

        detachedRequest.CancellationToken.IsCancellationRequested.Should().BeTrue();
        attachedRequest.CancellationToken.IsCancellationRequested.Should().BeFalse();
        coordinator.TryComplete(attachedRequest).Should().BeTrue();
    }

    [Fact]
    public void DisposeIsIdempotentAndRejectsNewRequests() {
        var coordinator = new ImageRequestCoordinator();

        coordinator.Dispose();
        coordinator.Dispose();

        Assert.Throws<ObjectDisposedException>(() => coordinator.Begin());
    }

    private sealed class TestImage : IImage, IDisposable {
        public bool IsDisposed { get; private set; }
        public Size Size => new(1, 1);

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect) {
        }

        public void Dispose() {
            IsDisposed = true;
        }
    }
}
