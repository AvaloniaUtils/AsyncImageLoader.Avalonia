using System.Threading;
using System.Threading.Tasks;

namespace AsyncImageLoader.Core;

/// <summary>
/// Stores decoded images by request key.
/// </summary>
public interface IImageCache {
    /// <summary>
    /// Attempts to acquire an image lease for a key.
    /// </summary>
    Task<IImageLease?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a decoded image for a key.
    /// </summary>
    Task SetAsync(string key, IImageLease image, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a key from the cache.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all entries from the cache.
    /// </summary>
    void Clear();
}
