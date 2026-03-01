using System.IO;
using System.Threading.Tasks;
using AsyncImageLoader.Memory.Services;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader.Loaders;

public class SmartDiskImageLoader : SmartImageLoader, ICoordinatedImageLoader {
    
    private readonly string _cacheFolder;
    
    public SmartDiskImageLoader(string cacheFolder = "Cache/Images/") {
        _cacheFolder = cacheFolder;
    }
    
    protected override Task<Bitmap?> LoadFromGlobalCache(string url) {
        var path = Path.Combine(_cacheFolder, CreateMD5(url));

        return File.Exists(path) ? Task.FromResult<Bitmap?>(new Bitmap(path)) : Task.FromResult<Bitmap?>(null);
    }
    
#if NETSTANDARD2_1
        protected sealed override async Task SaveToGlobalCache(string url, byte[] imageBytes) {
            using var memoryStream = new MemoryStream(imageBytes);
        
            var bitmap = new Bitmap(memoryStream);
            var entry = new BitmapEntry(url, bitmap);
        
            BitmapStore.Instance.TryAdd(entry);

            var path = Path.Combine(_cacheFolder, CreateMD5(url));

            Directory.CreateDirectory(_cacheFolder);
            await File.WriteAllBytesAsync(path, imageBytes).ConfigureAwait(false);
        }
#else
    protected sealed override Task SaveToGlobalCache(string url, byte[] imageBytes) {
        
        using var memoryStream = new MemoryStream(imageBytes);
        
        var bitmap = new Bitmap(memoryStream);
        var entry = new BitmapEntry(url, bitmap);
        
        BitmapStore.Instance.TryAdd(entry);
        
        var path = Path.Combine(_cacheFolder, CreateMD5(url));
        Directory.CreateDirectory(_cacheFolder);
        File.WriteAllBytes(path, imageBytes);
        
        return Task.CompletedTask;
    }
#endif
}