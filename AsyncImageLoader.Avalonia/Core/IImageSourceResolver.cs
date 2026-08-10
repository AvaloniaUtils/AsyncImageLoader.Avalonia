using System.Threading;
using System.Threading.Tasks;

namespace AsyncImageLoader.Core;

/// <summary>
/// Resolves a request from a non-network image source.
/// </summary>
public interface IImageSourceResolver {
    /// <summary>
    /// Attempts to resolve an image source.
    /// </summary>
    /// <param name="request">The image request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved source, or <see langword="null"/> when unsupported or missing.</returns>
    Task<ResolvedImageSource?> ResolveAsync(
        ImageLoadRequest request,
        CancellationToken cancellationToken = default);
}
