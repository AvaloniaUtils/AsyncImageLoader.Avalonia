using System;
using System.IO;
using System.Threading;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader.Core;

/// <summary>
/// Decodes Avalonia bitmaps from image streams.
/// </summary>
public sealed class BitmapDecoder : IBitmapDecoder {
    /// <inheritdoc />
    public Bitmap Decode(Stream stream, CancellationToken cancellationToken = default) {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        cancellationToken.ThrowIfCancellationRequested();
        return new Bitmap(stream);
    }
}
