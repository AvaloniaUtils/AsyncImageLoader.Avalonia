using System;
using System.Collections.Generic;
using System.Net.Http;

namespace AsyncImageLoader.Core;

/// <summary>
/// Builds an image loading pipeline from replaceable source, transport, decoder and cache strategies.
/// </summary>
public sealed class ImageLoaderPipelineBuilder {
    private IImageSourceResolver? _sourceResolver;
    private IImageTransport? _transport;
    private IBitmapDecoder? _decoder;
    private IImageMemoryCache? _memoryCache;
    private IImageByteCache? _byteCache;
    private HttpClient? _httpClient;
    private bool _disposeHttpClient;
    private bool _built;

    private ImageLoaderPipelineBuilder(IImageMemoryCache memoryCache, IImageByteCache? byteCache = null) {
        _memoryCache = memoryCache;
        _byteCache = byteCache;
    }

    /// <summary>
    /// Creates the preset used by <see cref="Loaders.BaseWebImageLoader"/>.
    /// </summary>
    public static ImageLoaderPipelineBuilder Uncached() {
        return new ImageLoaderPipelineBuilder(new TransientImageCache());
    }

    /// <summary>
    /// Creates the preset used by <see cref="Loaders.RamCachedWebImageLoader"/>.
    /// </summary>
    public static ImageLoaderPipelineBuilder RamCached(MemoryImageCacheOptions? options = null) {
        return new ImageLoaderPipelineBuilder(new MemoryImageCache(options));
    }

    internal static ImageLoaderPipelineBuilder RamCached(
        MemoryImageCacheOptions? options,
        TimeProvider timeProvider) {
        return new ImageLoaderPipelineBuilder(new MemoryImageCache(options, timeProvider));
    }

    /// <summary>
    /// Creates the preset used by <see cref="Loaders.DiskCachedWebImageLoader"/>.
    /// </summary>
    public static ImageLoaderPipelineBuilder DiskCached(
        string cacheFolder = "Cache/Images/",
        MemoryImageCacheOptions? memoryCacheOptions = null) {
        return new ImageLoaderPipelineBuilder(
            new MemoryImageCache(memoryCacheOptions),
            new DiskImageByteCache(cacheFolder));
    }

    /// <summary>
    /// Replaces source resolution for local files, storage providers and Avalonia assets.
    /// </summary>
    public ImageLoaderPipelineBuilder UseSourceResolver(IImageSourceResolver sourceResolver) {
        EnsureNotBuilt();
        _sourceResolver = sourceResolver ?? throw new ArgumentNullException(nameof(sourceResolver));
        return this;
    }

    /// <summary>
    /// Replaces external image transport. A configured HTTP client is ignored when a transport is supplied.
    /// </summary>
    public ImageLoaderPipelineBuilder UseTransport(IImageTransport transport) {
        EnsureNotBuilt();
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        return this;
    }

    /// <summary>
    /// Replaces bitmap decoding.
    /// </summary>
    public ImageLoaderPipelineBuilder UseDecoder(IBitmapDecoder decoder) {
        EnsureNotBuilt();
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        return this;
    }

    /// <summary>
    /// Replaces decoded image retention. Ownership is transferred to the built pipeline.
    /// </summary>
    public ImageLoaderPipelineBuilder UseMemoryCache(IImageMemoryCache memoryCache) {
        EnsureNotBuilt();
        ArgumentNullException.ThrowIfNull(memoryCache);
        _memoryCache?.Dispose();
        _memoryCache = memoryCache;
        return this;
    }

    /// <summary>
    /// Replaces or disables encoded byte caching.
    /// </summary>
    public ImageLoaderPipelineBuilder UseByteCache(IImageByteCache? byteCache) {
        EnsureNotBuilt();
        _byteCache = byteCache;
        return this;
    }

    /// <summary>
    /// Uses an HTTP client for the default transport.
    /// </summary>
    /// <param name="httpClient">The client to use.</param>
    /// <param name="disposeHttpClient">Whether the built pipeline takes ownership of the client.</param>
    public ImageLoaderPipelineBuilder UseHttpClient(HttpClient httpClient, bool disposeHttpClient = false) {
        EnsureNotBuilt();
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeHttpClient = disposeHttpClient;
        return this;
    }

    /// <summary>
    /// Builds the configured pipeline. A builder can build only one pipeline because cache ownership is transferred.
    /// </summary>
    public ImageLoaderPipeline Build() {
        EnsureNotBuilt();
        _built = true;

        var ownedResources = new List<IDisposable>();
        var transport = _transport;
        if (transport is null) {
            var httpClient = _httpClient;
            if (httpClient is null) {
                httpClient = new HttpClient();
                ownedResources.Add(httpClient);
            }
            else if (_disposeHttpClient) {
                ownedResources.Add(httpClient);
            }

            transport = new HttpImageTransport(httpClient);
        }

        return new ImageLoaderPipeline(
            _sourceResolver ?? CreateDefaultSourceResolver(),
            transport,
            _decoder ?? new BitmapDecoder(),
            _memoryCache!,
            _byteCache,
            ownedResources);
    }

    private static IImageSourceResolver CreateDefaultSourceResolver() {
        return new CompositeImageSourceResolver(
            new FileImageSourceResolver(),
            new StorageImageSourceResolver(),
            new AvaloniaAssetSourceResolver());
    }

    private void EnsureNotBuilt() {
        if (_built)
            throw new InvalidOperationException("This builder has already built a pipeline.");
    }
}
