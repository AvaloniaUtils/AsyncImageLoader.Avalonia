using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using AsyncImageLoader.Core.Caching;
using AsyncImageLoader.Core.Leases;
using AsyncImageLoader.Core.Pipeline;

namespace AsyncImageLoader.Loaders;

/// <summary>
/// Provides image loading with RAM and disk byte caches.
/// </summary>
[Obsolete("Use ImageLoaderPipelineBuilder.DiskCached(...).Build() instead.")]
public sealed class DiskCachedWebImageLoader : IAsyncImageLoader {
    private readonly bool _disposeHttpClient;
    private readonly HttpClient _httpClient;
    private readonly ImageLoaderPipeline _pipeline;

    /// <summary>
    /// Initializes a disk-cached loader.
    /// </summary>
    public DiskCachedWebImageLoader(string cacheFolder = "Cache/Images/")
        : this(new HttpClient(), true, null, cacheFolder) {
    }

    /// <summary>
    /// Initializes a disk-cached loader with RAM options.
    /// </summary>
    public DiskCachedWebImageLoader(MemoryImageCacheOptions options, string cacheFolder = "Cache/Images/")
        : this(new HttpClient(), true, options, cacheFolder) {
    }

    /// <summary>
    /// Initializes a disk-cached loader with a caller-provided HTTP client.
    /// </summary>
    public DiskCachedWebImageLoader(
        HttpClient httpClient,
        bool disposeHttpClient,
        string cacheFolder = "Cache/Images/")
        : this(httpClient, disposeHttpClient, null, cacheFolder) {
    }

    /// <summary>
    /// Initializes a disk-cached loader with a client and RAM options.
    /// </summary>
    public DiskCachedWebImageLoader(
        HttpClient httpClient,
        bool disposeHttpClient,
        MemoryImageCacheOptions? options,
        string cacheFolder = "Cache/Images/") {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (string.IsNullOrWhiteSpace(cacheFolder))
            throw new ArgumentException("Cache folder cannot be empty.", nameof(cacheFolder));

        _disposeHttpClient = disposeHttpClient;
        _pipeline = ImageLoaderPipelineBuilder.DiskCached(cacheFolder, options)
            .UseHttpClient(_httpClient)
            .Build();
    }

    /// <inheritdoc />
    public Task<IImageLease?> LoadAsync(
        ImageLoadRequest request,
        CancellationToken cancellationToken = default) {
        return _pipeline.LoadAsync(request, cancellationToken);
    }

    /// <summary>
    /// Clears decoded images from the RAM cache while retaining persisted disk data.
    /// </summary>
    public void ClearRamCache() {
        _pipeline.ClearMemoryCache();
    }

    /// <inheritdoc />
    public void Dispose() {
        _pipeline.Dispose();
        if (_disposeHttpClient)
            _httpClient.Dispose();
    }
}
