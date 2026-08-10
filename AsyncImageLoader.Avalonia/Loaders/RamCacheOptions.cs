using System;

namespace AsyncImageLoader.Loaders;

/// <summary>
///     Specifies how long a bitmap is strongly retained by <see cref="RamCachedWebImageLoader" />.
/// </summary>
public sealed class RamCacheOptions {
    /// <summary>
    ///     Gets or sets the maximum time an entry can be strongly retained, regardless of accesses.
    /// </summary>
    public TimeSpan? AbsoluteExpiration { get; init; }

    /// <summary>
    ///     Gets or sets the time after which an entry is weakly retained when it is not accessed.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; init; }

    internal void Validate() {
        if (AbsoluteExpiration is { } absoluteExpiration && absoluteExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(AbsoluteExpiration), "The expiration must be positive.");

        if (SlidingExpiration is { } slidingExpiration && slidingExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(SlidingExpiration), "The expiration must be positive.");
    }
}
