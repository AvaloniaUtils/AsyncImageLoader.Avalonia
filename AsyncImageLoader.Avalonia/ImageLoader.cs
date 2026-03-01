using System;
using System.Threading.Tasks;
using System.Threading;
using AsyncImageLoader.Loaders;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System.Collections.Concurrent;
using AsyncImageLoader.Memory;
using AsyncImageLoader.Memory.Services;
using Avalonia.Logging;

namespace AsyncImageLoader;

public static class ImageLoader {
    private static readonly ParametrizedLogger? Logger;
    public const string AsyncImageLoaderLogArea = "AsyncImageLoader";

    public static readonly AttachedProperty<string?> SourceProperty =
        AvaloniaProperty.RegisterAttached<Image, string?>("Source", typeof(ImageLoader));

    public static readonly AttachedProperty<bool> IsLoadingProperty =
        AvaloniaProperty.RegisterAttached<Image, bool>("IsLoading", typeof(ImageLoader));

    static ImageLoader() {
        SourceProperty.Changed.AddClassHandler<Image>(OnSourceChanged);
        Logger = Avalonia.Logging.Logger.TryGet(LogEventLevel.Error, AsyncImageLoaderLogArea);
    }
    
    public static IAsyncImageLoader AsyncImageLoader { get; set; } = new RamCachedWebImageLoader();
    
    public static BitmapCacheCoordinator BitmapCacheEvictionManager { get; set; } = 
        new (new VisibilityTimeoutPolicy(TimeSpan.FromSeconds(20)));

    private static readonly ConcurrentDictionary<Image, CancellationTokenSource> PendingOperations = new();

    private static async void OnSourceChanged(Image sender, AvaloniaPropertyChangedEventArgs args)
    {
        var url = args.GetNewValue<string?>();

        var cts = PendingOperations.AddOrUpdate(sender, new CancellationTokenSource(),
            (x, y) => {
                y.Cancel();
                y.Dispose();
                return new CancellationTokenSource();
            });

        if (string.IsNullOrWhiteSpace(url))
        {
            if (PendingOperations.TryRemove(sender, out var removedCts))
                removedCts.Dispose();

            if (sender.Source is Bitmap oldBmp)
                oldBmp.Dispose();

            sender.Source = null;
            return;
        }

        SetIsLoading(sender, true);

        Bitmap? bitmap = null;

        try {
            if (AsyncImageLoader is ICoordinatedImageLoader coordinatedImageLoader) {
                var entry = await coordinatedImageLoader.CoordinatorProvideImageAsync(url);
                
                if(entry != null)
                    entry.Acquire();
                
                bitmap = entry?.Bitmap;
            }
            else if (AsyncImageLoader is IAdvancedAsyncImageLoader advancedLoader)
                bitmap = await advancedLoader.ProvideImageAsync(url, TopLevel.GetTopLevel(sender)?.StorageProvider);
            else
                bitmap = await AsyncImageLoader.ProvideImageAsync(url);
        }
        catch (TaskCanceledException) { }
        catch (Exception e)
        {
            Logger?.Log(LogEventLevel.Error, "ImageLoader image resolution failed: {0}", e);
        }

        if (!cts.Token.IsCancellationRequested && bitmap != null)
        {
            if (sender.Source is Bitmap oldBmp)
                oldBmp.Dispose();

            sender.Source = bitmap;
        }
        else
        {
            bitmap?.Dispose();
        }

        if (PendingOperations.TryRemove(sender, out var removedCtsFinal))
            removedCtsFinal.Dispose();

        SetIsLoading(sender, false);
    }


    public static string? GetSource(Image element) {
        return element.GetValue(SourceProperty);
    }

    public static void SetSource(Image element, string? value) {
        element.SetValue(SourceProperty, value);
    }

    public static bool GetIsLoading(Image element) {
        return element.GetValue(IsLoadingProperty);
    }

    private static void SetIsLoading(Image element, bool value) {
        element.SetValue(IsLoadingProperty, value);
    }
}