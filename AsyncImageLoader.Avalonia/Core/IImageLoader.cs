using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncImageLoader.Core;

/// <summary>
/// Public boundary for image loading implementations.
/// </summary>
public interface IImageLoader : IDisposable {
    /// <summary>
    /// Loads an image and returns a lease owned by the caller.
    /// </summary>
    Task<IImageLease?> LoadAsync(
        ImageLoadRequest request,
        CancellationToken cancellationToken = default);
}
