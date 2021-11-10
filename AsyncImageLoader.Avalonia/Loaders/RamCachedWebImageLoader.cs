using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader.Loaders {
    /// <summary>
    /// Provides memory cached way to asynchronously load images for <see cref="ImageLoader"/>
    /// Can be used as base class if you want to create custom in memory caching
    /// </summary>
    public class RamCachedWebImageLoader : BaseWebImageLoader {
        private readonly ConcurrentDictionary<string, Task<IBitmap?>> _memoryCache = new();

        public RamCachedWebImageLoader() { }
        public RamCachedWebImageLoader(HttpClient httpClient) : base(httpClient) { }

        /// <inheritdoc />
        public override async Task<IBitmap?> ProvideImageAsync(string url) {
            var bitmap = await _memoryCache.GetOrAdd(url, LoadAsync);
            // If load failed - remove from cache and return
            // Next load attempt will try to load image again
            if (bitmap == null) _memoryCache.TryRemove(url, out _);
            return bitmap;
        }
    }
}