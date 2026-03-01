using System;
using System.Threading.Tasks;
using AsyncImageLoader.Memory.Services;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace AsyncImageLoader;

public interface IAsyncImageLoader : IDisposable {
    /// <summary>
    ///     Loads image
    /// </summary>
    /// <param name="url">Target url</param>
    /// <returns>Bitmap</returns>
    public Task<Bitmap?> ProvideImageAsync(string url);
}

public interface ICoordinatedImageLoader 
{
    public Task<BitmapEntry?> CoordinatorProvideImageAsync(string url);
    
    public Task<BitmapEntry?> CoordinatorProvideImageAsync(string url, IStorageProvider? storageProvider = null);
}