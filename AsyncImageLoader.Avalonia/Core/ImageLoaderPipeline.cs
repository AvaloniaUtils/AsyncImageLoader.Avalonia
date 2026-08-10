using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncImageLoader.Core;

/// <summary>
/// Composes source resolution, transport, decoding and image caches.
/// </summary>
public sealed class ImageLoaderPipeline : global::AsyncImageLoader.IAsyncImageLoader {
    private readonly IImageSourceResolver _sourceResolver;
    private readonly IImageTransport _transport;
    private readonly IBitmapDecoder _decoder;
    private readonly IImageMemoryCache _memoryCache;
    private readonly IImageByteCache? _byteCache;
    private bool _disposed;

    /// <summary>
    /// Initializes an image loading pipeline.
    /// </summary>
    public ImageLoaderPipeline(
        IImageSourceResolver sourceResolver,
        IImageTransport transport,
        IBitmapDecoder decoder,
        IImageMemoryCache memoryCache,
        IImageByteCache? byteCache = null) {
        _sourceResolver = sourceResolver ?? throw new ArgumentNullException(nameof(sourceResolver));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _byteCache = byteCache;
    }

    /// <summary>
    /// Loads and leases an image for the specified request.
    /// </summary>
    public Task<IImageLease?> LoadAsync(
        ImageLoadRequest request,
        CancellationToken cancellationToken = default) {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (_disposed)
            throw new ObjectDisposedException(nameof(ImageLoaderPipeline));

        return _memoryCache.GetOrCreateAsync(
            CreateCacheKey(request),
            token => LoadImageAsync(request, token),
            cancellationToken);
    }

    /// <summary>
    /// Releases pipeline-owned cache resources.
    /// </summary>
    public void Dispose() {
        if (_disposed)
            return;

        _disposed = true;
        _memoryCache.Dispose();
    }

    /// <summary>
    /// Clears unleased decoded images from the memory cache.
    /// </summary>
    public void ClearMemoryCache() {
        _memoryCache.Clear();
    }

    private async Task<Avalonia.Media.IImage?> LoadImageAsync(
        ImageLoadRequest request,
        CancellationToken cancellationToken) {
        using var resolved = await _sourceResolver.ResolveAsync(request, cancellationToken)
            .ConfigureAwait(false);
        if (resolved is not null)
            return _decoder.Decode(resolved.Stream, cancellationToken);

        using var encoded = await GetExternalDataAsync(request, cancellationToken).ConfigureAwait(false);
        if (encoded is null)
            return null;

        return _decoder.Decode(encoded.Stream, cancellationToken);
    }

    private async Task<ResolvedImageSource?> GetExternalDataAsync(
        ImageLoadRequest request,
        CancellationToken cancellationToken) {
        if (_byteCache is not null && IsHttpSource(request.Source)) {
            var cached = await _byteCache.GetAsync(CreateCacheKey(request), cancellationToken).ConfigureAwait(false);
            if (cached is not null)
                return new ResolvedImageSource(cached);
        }

        Stream? responseStream;
        try {
            responseStream = await _transport.GetAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception) {
            return null;
        }
        if (responseStream is null)
            return null;

        if (_byteCache is null || !IsHttpSource(request.Source))
            return new ResolvedImageSource(responseStream);

        var buffered = new MemoryStream();
        try {
            await responseStream.CopyToAsync(buffered, cancellationToken).ConfigureAwait(false);
            responseStream.Dispose();
            buffered.Position = 0;

            try {
                await _byteCache.SetAsync(CreateCacheKey(request), buffered, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            }
            catch (IOException) {
                // Persistence is best effort; the downloaded image remains usable.
            }
            catch (UnauthorizedAccessException) {
                // Persistence is best effort; the downloaded image remains usable.
            }

            buffered.Position = 0;
            return new ResolvedImageSource(buffered);
        }
        catch {
            await buffered.DisposeAsync();
            await responseStream.DisposeAsync();
            throw;
        }
    }

    private static string CreateCacheKey(ImageLoadRequest request) {
        return request.Source;
    }

    private static bool IsHttpSource(string source) {
        return Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
