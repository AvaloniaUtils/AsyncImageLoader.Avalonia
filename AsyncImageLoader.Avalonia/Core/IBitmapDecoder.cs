using System.IO;
using System.Threading;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader.Core;

/// <summary>
/// Creates a bitmap from resolved image data.
/// </summary>
public interface IBitmapDecoder {
    /// <summary>
    /// Decodes a bitmap from the supplied stream.
    /// </summary>
    /// <param name="stream">The image data stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The decoded bitmap.</returns>
    Bitmap Decode(Stream stream, CancellationToken cancellationToken = default);
}
