using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader.Core;

/// <summary>
/// Decodes Avalonia bitmaps from image streams.
/// </summary>
public sealed class BitmapDecoder : IBitmapDecoder {
    /// <inheritdoc />
    public async Task<Bitmap> DecodeAsync(Stream stream, CancellationToken cancellationToken = default) {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        cancellationToken.ThrowIfCancellationRequested();
        if (stream.CanSeek)
            return new Bitmap(stream);

        using var buffered = new MemoryStream();
        await stream.CopyToAsync(buffered, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        buffered.Position = 0;
        return new Bitmap(buffered);
    }
}
