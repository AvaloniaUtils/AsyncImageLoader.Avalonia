using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader.Avalonia {
    public interface IAsyncImageLoader {
        /// <summary>
        /// Loads image
        /// </summary>
        /// <param name="url">Target url</param>
        /// <returns>Bitmap</returns>
        public Task<IBitmap?> ProvideImageAsync(string url);
    }
}