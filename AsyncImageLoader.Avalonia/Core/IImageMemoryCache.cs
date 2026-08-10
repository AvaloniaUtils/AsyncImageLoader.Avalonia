using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;

namespace AsyncImageLoader.Core;

/// <summary>
/// Provides shared ownership and caching for decoded images.
/// </summary>
public interface IImageMemoryCache : IDisposable {
    /// <summary>
    /// Gets a cached image or creates it once for concurrent callers.
    /// </summary>
    Task<IImageLease?> GetOrCreateAsync(
        string key,
        Func<CancellationToken, Task<IImage?>> factory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all entries that are not currently leased.
    /// </summary>
    void Clear();
}
