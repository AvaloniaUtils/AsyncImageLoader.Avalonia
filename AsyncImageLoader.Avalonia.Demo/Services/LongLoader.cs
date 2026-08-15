using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using AsyncImageLoader.Loaders;

namespace AsyncImageLoader.Avalonia.Demo.Services;

public sealed class LongLoader : IAsyncImageLoader {
    public static LongLoader Instance { get; } = new();
    private readonly BaseWebImageLoader _inner = new();

    public async Task<IImageLease?> LoadAsync(ImageLoadRequest request, CancellationToken cancellationToken = default) {
        await Task.Delay(1000);
        return await _inner.LoadAsync(request, cancellationToken);
    }

    public void Dispose() {
        _inner.Dispose();
    }
}
