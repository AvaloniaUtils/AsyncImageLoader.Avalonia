using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using AsyncImageLoader.Memory.Services;
using Avalonia.Platform.Storage;

namespace AsyncImageLoader.Loaders;

public class SmartImageLoader : BaseWebImageLoader, ICoordinatedImageLoader
{
    private readonly ConcurrentDictionary<string, Task<byte[]?>> _loadingTasks = new();

    protected override async Task<byte[]?> LoadDataFromExternalAsync(string url)
    {
        var task = _loadingTasks.GetOrAdd(url, GetImageFromExternalAsync);

        try
        {
            return await task.ConfigureAwait(false);
        }
        finally
        {
            _loadingTasks.TryRemove(url, out _);
        }
    }

    private async Task<byte[]?> GetImageFromExternalAsync(string url)
    {
        try
        {
            using var response = await HttpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, url),
                HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var bytes = await response.Content
                .ReadAsByteArrayAsync()
                .ConfigureAwait(false);
            
            return bytes;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            Logger.Value.Log(
                "Failed to resolve image from request with uri: {0}\nException: {1}",
                url, e);
            return null;
        }
    }

    public async Task<BitmapEntry?> CoordinatorProvideImageAsync(string url) {
        if (BitmapStore.Instance.TryGet(url, out var entry))
            return entry;

        var bitmap = await LoadAsync(url)
            .ConfigureAwait(false);
        
        if(bitmap == null)
            return null;
        
        entry = new BitmapEntry(url, bitmap);
        
        BitmapStore.Instance.TryAdd(entry);
        
        return entry;
    }

    public async Task<BitmapEntry?> CoordinatorProvideImageAsync(string url, IStorageProvider? storageProvider = null) {
        if (BitmapStore.Instance.TryGet(url, out var entry))
            return entry;

        var bitmap = await LoadAsync(url, storageProvider)
            .ConfigureAwait(false);
        
        if(bitmap == null)
            return null;
        
        entry = new BitmapEntry(url, bitmap);
        
        BitmapStore.Instance.TryAdd(entry);
        
        return entry;
    }
}