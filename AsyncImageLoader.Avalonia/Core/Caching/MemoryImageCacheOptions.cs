using System;

namespace AsyncImageLoader.Core.Caching;

/// <summary>
/// Configures the decoded image memory cache.
/// </summary>
public sealed class MemoryImageCacheOptions {
    /// <summary>
    /// Gets the maximum time an entry can be retained, regardless of access.
    /// </summary>
    public TimeSpan? AbsoluteExpiration { get; init; }

    /// <summary>
    /// Gets the time after which an unaccessed entry becomes eligible for cleanup.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; init; }

    /// <summary>
    /// Gets the maximum number of images retained by the cache.
    /// </summary>
    public uint? MaxItems { get; init; }

    /// <summary>
    /// Validates cache options.
    /// </summary>
    public void Validate() {
        if (AbsoluteExpiration is { } absolute && absolute <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(AbsoluteExpiration));

        if (SlidingExpiration is { } sliding && sliding <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(SlidingExpiration));

        if (MaxItems is 0)
            throw new ArgumentOutOfRangeException(nameof(MaxItems));
    }
}
