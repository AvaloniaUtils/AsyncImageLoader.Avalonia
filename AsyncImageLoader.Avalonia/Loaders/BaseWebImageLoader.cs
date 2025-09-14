using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using IStorageProvider = Avalonia.Platform.Storage.IStorageProvider;

namespace AsyncImageLoader.Loaders;

/// <summary>
///     Provides non cached way to asynchronously load images for <see cref="ImageLoader" />
///     Can be used as base class if you want to create custom caching mechanism
/// </summary>
public class BaseWebImageLoader : IAsyncImageLoader, IAdvancedAsyncImageLoader {
    private readonly ParametrizedLogger? _logger;
    private readonly bool _shouldDisposeHttpClient;

    /// <summary>
    ///     Initializes a new instance with new <see cref="HttpClient" /> instance
    /// </summary>
    public BaseWebImageLoader() : this(new HttpClient(), true) { }

    /// <summary>
    ///     Initializes a new instance with the provided <see cref="HttpClient" />, and specifies whether that
    ///     <see cref="HttpClient" /> should be disposed when this instance is disposed.
    /// </summary>
    /// <param name="httpClient">The HttpMessageHandler responsible for processing the HTTP response messages.</param>
    /// <param name="disposeHttpClient">
    ///     true if the inner handler should be disposed of by Dispose; false if you intend to
    ///     reuse the HttpClient.
    /// </param>
    public BaseWebImageLoader(HttpClient httpClient, bool disposeHttpClient) {
        HttpClient = httpClient;
        _shouldDisposeHttpClient = disposeHttpClient;
        _logger = Logger.TryGet(LogEventLevel.Error, ImageLoader.AsyncImageLoaderLogArea);
    }

    protected HttpClient HttpClient { get; }

    /// <inheritdoc />
    public virtual async Task<Bitmap?> ProvideImageAsync(string url) {
        return await LoadAsync(url).ConfigureAwait(false);
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async Task<Bitmap?> ProvideImageAsync(string url, IStorageProvider? storageProvider = null) {
        return await LoadAsync(url, storageProvider).ConfigureAwait(false);
    }

    /// <summary>
    ///     Attempts to load bitmap
    /// </summary>
    /// <param name="url">Target url</param>
    /// <param name="storageProvider">Avalonia's storage provider</param>
    /// <returns>Bitmap</returns>
    protected virtual async Task<Bitmap?> LoadAsync(string url, IStorageProvider? storageProvider) {
        var internalOrCachedBitmap =
            await LoadFromLocalAsync(url, storageProvider).ConfigureAwait(false)
            ?? await LoadFromInternalAsync(url).ConfigureAwait(false)
            ?? await LoadFromGlobalCache(url).ConfigureAwait(false);
        if (internalOrCachedBitmap != null) return internalOrCachedBitmap;

        try {
            var externalBytes = await LoadDataFromExternalAsync(url).ConfigureAwait(false);
            if (externalBytes == null) return null;

            using var memoryStream = new MemoryStream(externalBytes);
            var bitmap = new Bitmap(memoryStream);
            await SaveToGlobalCache(url, externalBytes).ConfigureAwait(false);
            return bitmap;
        }
        catch (Exception e) {
            _logger?.Log(this, "Failed to resolve image: {RequestUri}\nException: {Exception}", url, e);

            return null;
        }
    }

    /// <summary>
    ///     Attempts to load bitmap
    /// </summary>
    /// <param name="url">Target url</param>
    /// <returns>Bitmap</returns>
    protected virtual Task<Bitmap?> LoadAsync(string url) {
        return LoadAsync(url, null);
    }

    /// <summary>
    /// The url maybe is local file url, so if file exists, we got a Bitmap
    /// </summary>
    /// <param name="url">Url to load</param>
    /// <param name="storageProvider">Avalonia's storage provider</param>
    private async Task<Bitmap?> LoadFromLocalAsync(string url, IStorageProvider? storageProvider) {
        if (File.Exists(url))
            return new Bitmap(url);

        if (storageProvider is null) return null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("file" or "content")) return null;

        try {
            var fileInfo = await storageProvider.TryGetFileFromPathAsync(uri);
            if (fileInfo is null) return null;
            using var fileStream = await fileInfo.OpenReadAsync();
            return new Bitmap(fileStream);
        }
        catch (Exception e) {
            _logger?.Log(this,
                "Failed to resolve local image via storage provider with uri: {RequestUri}\nException: {Exception}",
                url, e);
            return null;
        }
    }

    /// <summary>
    ///     Receives image bytes from an internal source (for example, from the disk).
    ///     This data will be NOT cached globally (because it is assumed that it is already in internal source us and does not
    ///     require global caching)
    /// </summary>
    /// <param name="url">Target url</param>
    /// <returns>Bitmap</returns>
    protected virtual Task<Bitmap?> LoadFromInternalAsync(string url) {
        try {
            var uri = url.StartsWith("/")
                ? new Uri(url, UriKind.Relative)
                : new Uri(url, UriKind.RelativeOrAbsolute);

            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                return Task.FromResult<Bitmap?>(null);

            if (uri is { IsAbsoluteUri: true, IsFile: true })
                return Task.FromResult(new Bitmap(uri.LocalPath))!;

            return Task.FromResult(new Bitmap(AssetLoader.Open(uri)))!;
        }
        catch (Exception e) {
            _logger?.Log(this,
                "Failed to resolve image from request with uri: {RequestUri}\nException: {Exception}", url, e);
            return Task.FromResult<Bitmap?>(null);
        }
    }

    /// <summary>
    ///     Receives image bytes from an external source (for example, from the Internet).
    ///     This data will be cached globally (if required by the current implementation)
    /// </summary>
    /// <param name="url">Target url</param>
    /// <returns>Image bytes</returns>
    protected virtual async Task<byte[]?> LoadDataFromExternalAsync(string url) {
        try {
            return await HttpClient.GetByteArrayAsync(url).ConfigureAwait(false);
        }
        catch (Exception e) {
            _logger?.Log(this,
                "Failed to resolve image from request with uri: {RequestUri}\nException: {Exception}", url, e);
            return null;
        }
    }

    /// <summary>
    ///     Attempts to load image from global cache (if it is stored before)
    /// </summary>
    /// <param name="url">Target url</param>
    /// <returns>Bitmap</returns>
    protected virtual Task<Bitmap?> LoadFromGlobalCache(string url) {
        // Current implementation does not provide global caching
        return Task.FromResult<Bitmap?>(null);
    }

    /// <summary>
    ///     Attempts to load image from global cache (if it is stored before)
    /// </summary>
    /// <param name="url">Target url</param>
    /// <param name="imageBytes">Bytes to save</param>
    /// <returns>Bitmap</returns>
    protected virtual Task SaveToGlobalCache(string url, byte[] imageBytes) {
        // Current implementation does not provide global caching
        return Task.CompletedTask;
    }

    ~BaseWebImageLoader() {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing && _shouldDisposeHttpClient) HttpClient.Dispose();
    }
}