using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using AsyncImageLoader.Core.Leases;
using AsyncImageLoader.Core.Pipeline;

namespace AsyncImageLoader;

/// <summary>
/// Public boundary for image loading implementations.
/// </summary>
public interface IAsyncImageLoader : IDisposable {
    /// <summary>
    /// Loads an image and returns a lease owned by the caller.
    /// </summary>
    Task<IImageLease?> LoadAsync(
        ImageLoadRequest request,
        CancellationToken cancellationToken = default);
}
