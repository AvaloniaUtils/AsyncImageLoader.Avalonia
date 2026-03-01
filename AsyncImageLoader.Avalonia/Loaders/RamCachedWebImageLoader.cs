using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using AsyncImageLoader.Memory.Services;
using Avalonia.Media.Imaging;
using IStorageProvider = Avalonia.Platform.Storage.IStorageProvider;

namespace AsyncImageLoader.Loaders;

/// <summary>
///     Provides memory cached way to asynchronously load images for <see cref="ImageLoader" />
///     Can be used as base class if you want to create custom in memory caching
/// </summary>
public class RamCachedWebImageLoader : BaseWebImageLoader {
    private readonly ConcurrentDictionary<string, Task<BitmapEntry?>> _memoryCache = new();

    /// <inheritdoc />
    public RamCachedWebImageLoader() {  }

    /// <inheritdoc />
    public RamCachedWebImageLoader(HttpClient httpClient, bool disposeHttpClient) : base(httpClient,
        disposeHttpClient) {
    }

    /// <inheritdoc />
    public override async Task<Bitmap?> ProvideImageAsync(string url) {
        var entry = await _memoryCache.GetOrAdd(url, async (url) => {
                var bitmap = await LoadAsync(url);
                
                if(bitmap == null) 
                    return null;
                
                var lease = new BitmapEntry(url, bitmap);
                
                BitmapStore.Instance.AddBitmapEntry(lease);
                
                lease.Acquire();
                
                return lease;
            })
            .ConfigureAwait(false);
        
        
        // If load failed - remove from cache and return
        // Next load attempt will try to load image again
        if (entry == null) {
            _memoryCache.TryRemove(url, out _);
            BitmapStore.Instance.RemoveBitmapEntry(url);
        }
        
        return entry.Bitmap;
    }

    public override async Task<Bitmap?> ProvideImageAsync(string url, IStorageProvider? storageProvider = null) {
        var entry = await _memoryCache.GetOrAdd(url, async (url) => {
                var bitmap = await LoadAsync(url);
                
                if(bitmap == null) 
                    return null;
                
                var lease = new BitmapEntry(url, bitmap);
                
                BitmapStore.Instance.AddBitmapEntry(lease);
                
                lease.Acquire();
                
                return lease;
            })
            .ConfigureAwait(false);
        
        // If load failed - remove from cache and return
        // Next load attempt will try to load image again
        if (entry == null) {
            _memoryCache.TryRemove(url, out _);
            BitmapStore.Instance.RemoveBitmapEntry(url);
        }
        
        return entry.Bitmap;
    }

    public void ClearRamCache() {
        _memoryCache.Clear();
    }
}