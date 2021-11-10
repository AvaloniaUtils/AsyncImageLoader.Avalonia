using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader.Loaders {
    /// <summary>
    /// Provides non cached way to asynchronously load images for <see cref="ImageLoader"/>
    /// Can be used as base class if you want to create custom caching mechanism
    /// </summary>
    public class BaseWebImageLoader : IAsyncImageLoader {
        private readonly bool _shouldDisposeHttpClient;

        /// <summary>
        /// Initializes a new instance with new <see cref="HttpClient"/> instance
        /// </summary>
        public BaseWebImageLoader() : this(new HttpClient(null, true), true) { }
        
        /// <summary>
        /// Initializes a new instance with the provided <see cref="HttpClient"/>, and specifies whether that <see cref="HttpClient"/> should be disposed when this instance is disposed.
        /// </summary>
        /// <param name="httpClient">The HttpMessageHandler responsible for processing the HTTP response messages.</param>
        /// <param name="disposeHttpClient">true if the inner handler should be disposed of by Dispose; false if you intend to reuse the HttpClient.</param>
        public BaseWebImageLoader(HttpClient httpClient, bool disposeHttpClient) {
            HttpClient = httpClient;
            _shouldDisposeHttpClient = disposeHttpClient;
        }

        protected HttpClient HttpClient { get; }

        /// <inheritdoc />
        public virtual Task<IBitmap?> ProvideImageAsync(string url) {
            return LoadAsync(url);
        }

        /// <summary>
        /// Attempts to load bitmap
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Bitmap</returns>
        protected virtual async Task<IBitmap?> LoadAsync(string url) {
            var internalOrCachedBitmap = await LoadFromInternalAsync(url) ?? await LoadFromGlobalCache(url);
            if (internalOrCachedBitmap != null) return internalOrCachedBitmap;

            try {
                var externalBytes = await LoadDataFromExternalAsync(url);
                if (externalBytes == null) return null;

                using var memoryStream = new MemoryStream(externalBytes);
                var bitmap = new Bitmap(memoryStream);
                await SaveToGlobalCache(url, externalBytes);
                return bitmap;
            }
            catch (Exception) {
                return null;
            }
        }

        /// <summary>
        /// Receives image bytes from an internal source (for example, from the disk).
        /// This data will be NOT cached globally (because it is assumed that it is already in internal source us and does not require global caching)
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Bitmap</returns>
        protected virtual Task<Bitmap?> LoadFromInternalAsync(string url) {
            try {
                return Task.FromResult(new Bitmap(url))!;
            }
            catch (Exception) {
                return Task.FromResult<Bitmap?>(null);
            }
        }

        /// <summary>
        /// Receives image bytes from an external source (for example, from the Internet).
        /// This data will be cached globally (if required by the current implementation)
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Image bytes</returns>
        protected virtual async Task<byte[]?> LoadDataFromExternalAsync(string url) {
            try {
                return await HttpClient.GetByteArrayAsync(url);
            }
            catch (Exception) {
                return null;
            }
        }

        /// <summary>
        /// Attempts to load image from global cache (if it is stored before)
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Bitmap</returns>
        protected virtual Task<Bitmap?> LoadFromGlobalCache(string url) {
            // Current implementation does not provide global caching
            return Task.FromResult<Bitmap?>(null);
        }

        /// <summary>
        /// Attempts to load image from global cache (if it is stored before)
        /// </summary>
        /// <param name="url">Target url</param>
        /// <param name="imageBytes">Bytes to save</param>
        /// <returns>Bitmap</returns>
        protected virtual Task SaveToGlobalCache(string url, byte[] imageBytes) {
            // Current implementation does not provide global caching
            return Task.CompletedTask;
        }

        public void Dispose() {
            if (_shouldDisposeHttpClient) {
                HttpClient.Dispose();
            }
        }
    }
}