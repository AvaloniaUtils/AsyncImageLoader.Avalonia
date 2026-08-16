using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core.Leases;
using Avalonia.Media;

namespace AsyncImageLoader.Core.Caching;

/// <summary>
/// Decodes a new image for every request without retaining it in a cache.
/// </summary>
public sealed class TransientImageCache : IImageMemoryCache {
    private bool _disposed;

    /// <inheritdoc />
    public async Task<IImageLease?> GetOrCreateAsync(
        string key,
        Func<CancellationToken, Task<IImage?>> factory,
        CancellationToken cancellationToken = default) {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be empty.", nameof(key));
        ArgumentNullException.ThrowIfNull(factory);
        if (_disposed)
            throw new ObjectDisposedException(nameof(TransientImageCache));

        var image = await factory(cancellationToken).ConfigureAwait(false);
        return image is null ? null : ImageLease.Owned(image);
    }

    /// <inheritdoc />
    public void Clear() {
    }

    /// <inheritdoc />
    public void Dispose() {
        _disposed = true;
    }
}
