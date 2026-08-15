using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;

namespace AsyncImageLoader.Loaders;

/// <summary>
/// Provides image loading with a lease-aware in-memory cache.
/// </summary>
public sealed class RamCachedWebImageLoader : global::AsyncImageLoader.IAsyncImageLoader {
    private readonly bool _disposeHttpClient;
    private readonly HttpClient _httpClient;
    private readonly ImageLoaderPipeline _pipeline;

    /// <inheritdoc />
    public RamCachedWebImageLoader() : this(null) {
    }

    /// <summary>
    /// Initializes a loader with RAM retention options.
    /// </summary>
    public RamCachedWebImageLoader(RamCacheOptions? options) {
        _httpClient = new HttpClient();
        _disposeHttpClient = true;
        _pipeline = ImageLoaderPipelineBuilder.RamCached(CreateMemoryOptions(options))
            .UseHttpClient(_httpClient)
            .Build();
    }

    /// <summary>
    /// Initializes a loader with a caller-provided HTTP client.
    /// </summary>
    public RamCachedWebImageLoader(HttpClient httpClient, bool disposeHttpClient)
        : this(httpClient, disposeHttpClient, null) {
    }

    /// <summary>
    /// Initializes a loader with a client and RAM retention options.
    /// </summary>
    public RamCachedWebImageLoader(
        HttpClient httpClient,
        bool disposeHttpClient,
        RamCacheOptions? options)
        : this(httpClient, disposeHttpClient, options, TimeProvider.System) {
    }

    internal RamCachedWebImageLoader(
        HttpClient httpClient,
        bool disposeHttpClient,
        RamCacheOptions? options,
        TimeProvider timeProvider) {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeHttpClient = disposeHttpClient;
        _pipeline = ImageLoaderPipelineBuilder.RamCached(CreateMemoryOptions(options), timeProvider)
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
    /// Clears entries that are not held by active leases.
    /// </summary>
    public void ClearRamCache() {
        // The cache is intentionally owned by the pipeline and exposed through this facade
        // only for compatibility with the original loader API.
        _pipeline.ClearMemoryCache();
    }

    /// <inheritdoc />
    public void Dispose() {
        _pipeline.Dispose();
        if (_disposeHttpClient)
            _httpClient.Dispose();
    }

    private static MemoryImageCacheOptions CreateMemoryOptions(RamCacheOptions? options) {
        return new MemoryImageCacheOptions {
            AbsoluteExpiration = options?.AbsoluteExpiration,
            SlidingExpiration = options?.SlidingExpiration
        };
    }
}
