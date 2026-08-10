using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using AwesomeAssertions;
using Avalonia.Media;
using Avalonia;
using Xunit;

namespace AsyncImageLoader.Avalonia.Tests;

public sealed class CustomImageLoaderTests {
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task CustomLoaderCanIgnoreBuiltInPipeline() {
        using var loader = new CustomLoader();

        using var lease = await loader.LoadAsync(new ImageLoadRequest("custom://image"));

        lease.Should().NotBeNull();
        loader.Requests.Should().Be(1);
        lease!.Image.Should().BeOfType<TestImage>();
    }

    [Fact]
    public void OwnedLeaseDisposesImageExactlyOnce() {
        var image = new TestImage();
        var bitmap = image;
        var lease = ImageLease.Owned(bitmap);

        lease.Dispose();
        lease.Dispose();

        Assert.Throws<ObjectDisposedException>(() => bitmap.Size);
    }

    [Fact]
    public void CustomReleaseActionIsCalledOnce() {
        var bitmap = new TestImage();
        var releases = 0;
        var lease = ImageLease.Create(bitmap, () => releases++);

        lease.Dispose();
        lease.Dispose();

        releases.Should().Be(1);
        bitmap.Dispose();
    }

    [Fact]
    public void NonOwningLeaseDoesNotDisposeImage() {
        var bitmap = new TestImage();
        var lease = ImageLease.NonOwning(bitmap);

        lease.Dispose();

        bitmap.Size.Width.Should().Be(1);
        bitmap.Dispose();
    }

    private sealed class CustomLoader : IImageLoader {
        public int Requests { get; private set; }

        public Task<IImageLease?> LoadAsync(
            ImageLoadRequest request,
            CancellationToken cancellationToken = default) {
            Requests++;
            return Task.FromResult<IImageLease?>(ImageLease.Owned(new TestImage()));
        }

        public void Dispose() {
        }
    }

    private sealed class TestImage : IImage, IDisposable {
        private bool _disposed;

        public Size Size {
            get {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(TestImage));
                return new Size(1, 1);
            }
        }

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect) {
            if (_disposed)
                throw new ObjectDisposedException(nameof(TestImage));
        }

        public void Dispose() {
            _disposed = true;
        }
    }
}
