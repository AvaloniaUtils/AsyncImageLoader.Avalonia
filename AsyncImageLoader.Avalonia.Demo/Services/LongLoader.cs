using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using AsyncImageLoader.Core.Leases;
using AsyncImageLoader.Core.Pipeline;

namespace AsyncImageLoader.Avalonia.Demo.Services;

public sealed class LongLoader : IAsyncImageLoader {
    public static LongLoader Instance { get; } = new();
    private readonly ImageLoaderPipeline _inner = ImageLoaderPipelineBuilder.Uncached().Build();

    public async Task<IImageLease?> LoadAsync(ImageLoadRequest request, CancellationToken cancellationToken = default) {
        await Task.Delay(1000);
        return await _inner.LoadAsync(request, cancellationToken);
    }

    public void Dispose() {
        _inner.Dispose();
    }
}
