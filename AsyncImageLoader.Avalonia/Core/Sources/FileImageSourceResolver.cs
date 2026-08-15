using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core.Pipeline;

namespace AsyncImageLoader.Core.Sources;

/// <summary>
/// Resolves image sources that point to files on the local filesystem.
/// </summary>
public sealed class FileImageSourceResolver : IImageSourceResolver {
    /// <inheritdoc />
    public Task<ResolvedImageSource?> ResolveAsync(
        ImageLoadRequest request,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();

        var path = request.Source;
        if (Uri.TryCreate(request.Source, UriKind.Absolute, out var uri) && uri.IsFile)
            path = uri.LocalPath;

        if (!File.Exists(path))
            return Task.FromResult<ResolvedImageSource?>(null);

        return Task.FromResult<ResolvedImageSource?>(
            new ResolvedImageSource(File.OpenRead(path)));
    }
}
