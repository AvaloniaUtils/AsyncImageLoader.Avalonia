using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncImageLoader.Core;

/// <summary>
/// Resolves file and content URIs through an Avalonia storage provider.
/// </summary>
public sealed class StorageImageSourceResolver : IImageSourceResolver {
    /// <inheritdoc />
    public async Task<ResolvedImageSource?> ResolveAsync(
        ImageLoadRequest request,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.StorageProvider is null ||
            !Uri.TryCreate(request.Source, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeFile && uri.Scheme != "content"))
            return null;

        try {
            var file = await request.StorageProvider.TryGetFileFromPathAsync(uri).ConfigureAwait(false);
            if (file is null)
                return null;

            cancellationToken.ThrowIfCancellationRequested();
            var stream = await file.OpenReadAsync().ConfigureAwait(false);
            return new ResolvedImageSource(stream);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception) {
            return null;
        }
    }
}
