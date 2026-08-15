using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using AsyncImageLoader.Core.Leases;
using AsyncImageLoader.Core.Pipeline;

namespace AsyncImageLoader.Loaders;

/// <summary>
/// Provides uncached image loading using the composable default strategies.
/// </summary>
[Obsolete("Use ImageLoaderPipelineBuilder.Uncached().Build() instead.")]
public sealed class BaseWebImageLoader : IAsyncImageLoader {
    private readonly bool _disposeHttpClient;
    private readonly HttpClient _httpClient;
    private readonly ImageLoaderPipeline _pipeline;

    /// <summary>
    /// Initializes a loader with a new HTTP client.
    /// </summary>
    public BaseWebImageLoader() : this(new HttpClient(), true) {
    }

    /// <summary>
    /// Initializes a loader with a caller-provided HTTP client.
    /// </summary>
    public BaseWebImageLoader(HttpClient httpClient, bool disposeHttpClient) {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeHttpClient = disposeHttpClient;
        _pipeline = ImageLoaderPipelineBuilder.Uncached()
            .UseHttpClient(httpClient)
            .Build();
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
