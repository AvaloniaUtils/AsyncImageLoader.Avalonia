using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;

namespace AsyncImageLoader.Loaders;

/// <summary>
/// Provides image loading with RAM and disk byte caches.
/// </summary>
public sealed class DiskCachedWebImageLoader : global::AsyncImageLoader.IAsyncImageLoader {
    private readonly bool _disposeHttpClient;
    private readonly HttpClient _httpClient;
    private readonly ImageLoaderPipeline _pipeline;

    /// <summary>
    /// Initializes a disk-cached loader.
    /// </summary>
    public DiskCachedWebImageLoader(string cacheFolder = "Cache/Images/")
        : this(new HttpClient(), true, null, cacheFolder, true) {
    }

    /// <summary>
    /// Initializes a disk-cached loader with RAM options.
    /// </summary>
    public DiskCachedWebImageLoader(RamCacheOptions options, string cacheFolder = "Cache/Images/")
        : this(new HttpClient(), true, options, cacheFolder, true) {
    }

    /// <summary>
    /// Initializes a disk-cached loader with a caller-provided HTTP client.
    /// </summary>
    public DiskCachedWebImageLoader(
        HttpClient httpClient,
        bool disposeHttpClient,
        string cacheFolder = "Cache/Images/")
        : this(httpClient, disposeHttpClient, null, cacheFolder, true) {
    }

    /// <summary>
    /// Initializes a disk-cached loader with a client and RAM options.
    /// </summary>
    public DiskCachedWebImageLoader(
        HttpClient httpClient,
        bool disposeHttpClient,
        RamCacheOptions options,
        string cacheFolder = "Cache/Images/")
        : this(httpClient, disposeHttpClient, (RamCacheOptions?)options, cacheFolder, true) {
    }

    private DiskCachedWebImageLoader(
        HttpClient httpClient,
        bool disposeHttpClient,
        RamCacheOptions? options,
        string cacheFolder,
        bool initialize) {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (string.IsNullOrWhiteSpace(cacheFolder))
            throw new ArgumentException("Cache folder cannot be empty.", nameof(cacheFolder));

        _disposeHttpClient = disposeHttpClient;
        _pipeline = BaseWebImageLoader.CreatePipeline(
            _httpClient,
            new MemoryImageCache(new MemoryImageCacheOptions {
                AbsoluteExpiration = options?.AbsoluteExpiration,
                SlidingExpiration = options?.SlidingExpiration
            }),
            new DiskImageByteCache(cacheFolder));
    }

    /// <inheritdoc />
    public Task<IImageLease?> LoadAsync(
        ImageLoadRequest request,
        CancellationToken cancellationToken = default) {
        return _pipeline.LoadAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose() {
        _pipeline.Dispose();
        if (_disposeHttpClient)
            _httpClient.Dispose();
    }
}
