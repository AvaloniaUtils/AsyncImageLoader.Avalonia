using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core.Pipeline;

namespace AsyncImageLoader.Core.Transport;

/// <summary>
/// Retrieves image data from an external source.
/// </summary>
public interface IImageTransport {
    /// <summary>
    /// Downloads image data for the specified request.
    /// </summary>
    /// <param name="request">The image request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response stream, or <see langword="null"/> when the source is unsupported.</returns>
    Task<Stream?> GetAsync(
        ImageLoadRequest request,
        CancellationToken cancellationToken = default);
}
