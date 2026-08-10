using System;
using System.Threading;
using Avalonia.Media;

namespace AsyncImageLoader.Core;

/// <summary>
/// Creates leases for custom image loader implementations.
/// </summary>
public static class ImageLease {
    /// <summary>
    /// Creates a lease that disposes the image when released.
    /// </summary>
    public static IImageLease Owned(IImage image) {
        return Create(image, () => (image as IDisposable)?.Dispose());
    }

    /// <summary>
    /// Creates a lease that invokes a custom release action once.
    /// </summary>
    public static IImageLease Create(IImage image, Action release) {
        return new ActionImageLease(image, release);
    }

    /// <summary>
    /// Creates a lease that does not dispose the image when released.
    /// </summary>
    public static IImageLease NonOwning(IImage image) {
        return Create(image, static () => { });
    }

    private sealed class ActionImageLease : IImageLease {
        private readonly Action _release;
        private IImage? _image;
        private int _disposed;

        public ActionImageLease(IImage image, Action release) {
            _image = image ?? throw new ArgumentNullException(nameof(image));
            _release = release ?? throw new ArgumentNullException(nameof(release));
        }

        public IImage Image {
            get {
                var image = Volatile.Read(ref _image);
                if (Volatile.Read(ref _disposed) != 0 || image is null)
                    throw new ObjectDisposedException(nameof(ActionImageLease));

                return image;
            }
        }

        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            Interlocked.Exchange(ref _image, null);
            _release();
        }
    }
}
