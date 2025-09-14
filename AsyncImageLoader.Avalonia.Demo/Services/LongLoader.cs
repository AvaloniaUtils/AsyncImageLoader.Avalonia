using System.Threading.Tasks;
using AsyncImageLoader.Loaders;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader.Avalonia.Demo.Services;

public class LongLoader : BaseWebImageLoader {
    public static LongLoader Instance { get; } = new LongLoader();

    protected override async Task<Bitmap?> LoadAsync(string url) {
        await Task.Delay(1000);
        return await base.LoadAsync(url);
    }
}