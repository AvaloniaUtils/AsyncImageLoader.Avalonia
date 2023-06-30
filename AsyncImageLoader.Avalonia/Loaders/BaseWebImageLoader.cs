using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AsyncImageLoader.Loaders
{
    /// <summary>
    /// Provides non cached way to asynchronously load images for <see cref="ImageLoader"/>
    /// Can be used as base class if you want to create custom caching mechanism
    /// </summary>
    public class BaseWebImageLoader : IAsyncImageLoader
    {

        private readonly bool                _shouldDisposeHttpClient;
        private readonly ParametrizedLogger? _logger;

        /// <summary>
        /// Initializes a new instance with new <see cref="HttpClient"/> instance
        /// </summary>
        public BaseWebImageLoader() : this(new HttpClient(), true) { }

        /// <summary>
        /// Initializes a new instance with the provided <see cref="HttpClient"/>, and specifies whether that <see cref="HttpClient"/> should be disposed when this instance is disposed.
        /// </summary>
        /// <param name="httpClient">The HttpMessageHandler responsible for processing the HTTP response messages.</param>
        /// <param name="disposeHttpClient">true if the inner handler should be disposed of by Dispose; false if you intend to reuse the HttpClient.</param>
        public BaseWebImageLoader(HttpClient httpClient, bool disposeHttpClient)
        {
            HttpClient               = httpClient;
            _shouldDisposeHttpClient = disposeHttpClient;
            _logger                  = Logger.TryGet(LogEventLevel.Information, ImageLoader.AsyncImageLoaderLogArea);
        }

        protected HttpClient HttpClient { get; }

        /// <inheritdoc />
        public virtual Task<Bitmap?> ProvideImageAsync(string url) => LoadAsync(url);

        /// <summary>
        /// Attempts to load bitmap
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Bitmap</returns>
        protected virtual async Task<Bitmap?> LoadAsync(string url)
        {
            var internalOrCachedBitmap = await LoadFromInternalAsync(url) ?? await LoadFromGlobalCache(url);
            if (internalOrCachedBitmap != null) return internalOrCachedBitmap;

            try
            {
                var externalBytes = await LoadDataFromExternalAsync(url);
                if (externalBytes == null) return null;

                using var memoryStream = new MemoryStream(externalBytes);
                var       bitmap       = new Bitmap(memoryStream);
                await SaveToGlobalCache(url, externalBytes);
                return bitmap;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Receives image bytes from an internal source (for example, from the disk).
        /// This data will be NOT cached globally (because it is assumed that it is already in internal source us and does not require global caching)
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Bitmap</returns>
        protected virtual Task<Bitmap?> LoadFromInternalAsync(string url)
        {
            try
            {
                var uri = url.StartsWith("/") ? new Uri(url, UriKind.Relative) : new Uri(url, UriKind.RelativeOrAbsolute);

                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    return Task.FromResult<Bitmap?>(null);
                }

                if (uri.IsAbsoluteUri && uri.IsFile) return Task.FromResult(new Bitmap(uri.LocalPath))!;

                //var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                // return Task.FromResult(new Bitmap(assets?.Open(uri) ?? default));

                return Task.FromResult(new Bitmap(AssetLoader.Open(uri)) ?? default);
            }
            catch (Exception e)
            {
                _logger?.Log(this, "Failed to resolve image from request with uri: {RequestUri}\nException: {Exception}", url, e);
                return Task.FromResult<Bitmap?>(null);
            }
        }

        /// <summary>
        /// Receives image bytes from an external source (for example, from the Internet).
        /// This data will be cached globally (if required by the current implementation)
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Image bytes</returns>
        protected virtual async Task<byte[]?> LoadDataFromExternalAsync(string url)
        {
            try
            {
                return await HttpClient.GetByteArrayAsync(url);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to load image from global cache (if it is stored before)
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Bitmap</returns>
        protected virtual Task<Bitmap?> LoadFromGlobalCache(string url) =>

            // Current implementation does not provide global caching
            Task.FromResult<Bitmap?>(null);

        /// <summary>
        /// Attempts to load image from global cache (if it is stored before)
        /// </summary>
        /// <param name="url">Target url</param>
        /// <param name="imageBytes">Bytes to save</param>
        /// <returns>Bitmap</returns>
        protected virtual Task SaveToGlobalCache(string url, byte[] imageBytes) =>

            // Current implementation does not provide global caching
            Task.CompletedTask;

        ~BaseWebImageLoader() { Dispose(false); }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _shouldDisposeHttpClient)
            {
                HttpClient.Dispose();
            }
        }

    }
}