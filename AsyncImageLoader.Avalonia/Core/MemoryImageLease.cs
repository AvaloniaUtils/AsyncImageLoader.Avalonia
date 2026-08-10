using System;
using System.Threading;
using Avalonia.Media;

namespace AsyncImageLoader.Core;

internal sealed class MemoryImageLease : IImageLease {
    private readonly Action _release;
    private IImage? _image;
    private int _disposed;

    public MemoryImageLease(IImage image, Action release) {
        _image = image ?? throw new ArgumentNullException(nameof(image));
        _release = release ?? throw new ArgumentNullException(nameof(release));
    }

    public IImage Image {
        get {
            var image = Volatile.Read(ref _image);
            if (Volatile.Read(ref _disposed) != 0 || image is null)
                throw new ObjectDisposedException(nameof(MemoryImageLease));

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
