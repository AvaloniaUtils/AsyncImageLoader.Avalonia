using System;
using System.IO;

namespace AsyncImageLoader.Core.Sources;

/// <summary>
/// Represents image bytes and the stream ownership required by a decoder.
/// </summary>
public sealed class ResolvedImageSource : IDisposable {
    private Stream? _stream;

    /// <summary>
    /// Initializes a resolved image source.
    /// </summary>
    /// <param name="stream">The stream containing image data.</param>
    public ResolvedImageSource(Stream stream) {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    /// <summary>
    /// Gets the image data stream.
    /// </summary>
    public Stream Stream => _stream ?? throw new ObjectDisposedException(nameof(ResolvedImageSource));

    /// <summary>
    /// Releases the owned stream.
    /// </summary>
    public void Dispose() {
        _stream?.Dispose();
        _stream = null;
    }
}
