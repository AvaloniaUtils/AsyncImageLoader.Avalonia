using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader.Loaders {
    /// <summary>
    /// Provides memory and disk cached way to asynchronously load images for <see cref="ImageLoader"/>
    /// Can be used as base class if you want to create custom caching mechanism
    /// </summary>
    public class DiskCachedWebImageLoader : RamCachedWebImageLoader {
        private readonly string _cacheFolder;

        public DiskCachedWebImageLoader(string cacheFolder = "Cache/Images/") {
            _cacheFolder = cacheFolder;
        }
        public DiskCachedWebImageLoader(HttpClient httpClient, string cacheFolder = "Cache/Images/") : base(httpClient) {
            _cacheFolder = cacheFolder;
        }

        /// <inheritdoc />
        protected override Task<Bitmap?> LoadFromGlobalCache(string url) {
            var path = Path.Combine(_cacheFolder, CreateMD5(url));
            if (File.Exists(path)) {
                return Task.FromResult(new Bitmap(url))!;
            }
            return Task.FromResult<Bitmap?>(null);
        }

        protected override Task SaveToGlobalCache(string url, byte[] imageBytes) {
            var path = Path.Combine(_cacheFolder, CreateMD5(url));
            Directory.CreateDirectory(_cacheFolder);
#if NETSTANDARD2_1
            return File.WriteAllBytesAsync(path, imageBytes);
#else
            File.WriteAllBytes(path, imageBytes);
            return Task.CompletedTask;
#endif
        }

        protected static string CreateMD5(string input) {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++) {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}