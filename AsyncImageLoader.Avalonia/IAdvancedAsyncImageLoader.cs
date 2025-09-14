using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace AsyncImageLoader;

public interface IAdvancedAsyncImageLoader : IDisposable {
    /// <summary>
    ///     Loads image
    /// </summary>
    /// <param name="url">Target url</param>
    /// <param name="storageProvider">Avalonia's storage provider</param>
    /// <returns>Bitmap</returns>
    public Task<Bitmap?> ProvideImageAsync(string url, IStorageProvider? storageProvider = null);
}