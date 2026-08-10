using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncImageLoader.Core;

/// <summary>
/// Stores encoded image data by cache key.
/// </summary>
public interface IImageByteCache {
    /// <summary>
    /// Gets encoded image data for a key, or <see langword="null"/> when it is not cached.
    /// </summary>
    Task<Stream?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores encoded image data for a key.
    /// </summary>
    Task SetAsync(string key, Stream data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cached key.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cached data.
    /// </summary>
    void Clear();
}
