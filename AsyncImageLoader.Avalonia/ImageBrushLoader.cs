using System;
using System.Collections.Concurrent;
using System.Threading;
using AsyncImageLoader.Loaders;
using AsyncImageLoader.Memory.Services;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader;

public static class ImageBrushLoader {
    private static readonly ParametrizedLogger? Logger;
    public static IAsyncImageLoader AsyncImageLoader { get; set; } = new RamCachedWebImageLoader();

    private static readonly ConcurrentDictionary<ImageBrush, CancellationTokenSource> PendingOperations = new();
    
    static ImageBrushLoader() {
        SourceProperty.Changed.AddClassHandler<ImageBrush>(OnSourceChanged);
        Logger = Avalonia.Logging.Logger.TryGet(LogEventLevel.Error, ImageLoader.AsyncImageLoaderLogArea);
    }

    private static async void OnSourceChanged(ImageBrush imageBrush, AvaloniaPropertyChangedEventArgs args)
    {
        var (oldValue, newValue) = args.GetOldAndNewValue<string?>();
        if (oldValue == newValue)
            return;
        
        var cts = PendingOperations.AddOrUpdate(imageBrush, _ => new CancellationTokenSource(),
            (_, oldCts) =>
            {
                oldCts.Cancel();
                oldCts.Dispose();
                return new CancellationTokenSource();
            });

        SetIsLoading(imageBrush, true);

        Bitmap? bitmap = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(newValue))
            {
                if (BitmapStore.Instance.TryGet(newValue, out var entry))
                    bitmap = entry.Bitmap;
                else 
                {
                    bitmap = await AsyncImageLoader.ProvideImageAsync(newValue);
                }
            
                if(AsyncImageLoader is ICoordinatedImageLoader)
                    if (bitmap != null) 
                        BitmapStore.Instance.TryAdd(new BitmapEntry(newValue, bitmap), true);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Logger?.Log("ImageBrushLoader", "ImageBrushLoader image resolution failed: {0}", e);
        }

        if (!cts.Token.IsCancellationRequested && GetSource(imageBrush) == newValue)
        {
            if (imageBrush.Source is Bitmap oldBmp)
                oldBmp.Dispose();

            imageBrush.Source = bitmap;
        }
        else
        {
            bitmap?.Dispose();
        }

        if (PendingOperations.TryRemove(imageBrush, out var removedCts))
            removedCts.Dispose();

        SetIsLoading(imageBrush, false);
    }

    public static readonly AttachedProperty<string?> SourceProperty =
        AvaloniaProperty.RegisterAttached<ImageBrush, string?>("Source", typeof(ImageLoader));

    public static string? GetSource(ImageBrush element) {
        return element.GetValue(SourceProperty);
    }

    public static void SetSource(ImageBrush element, string? value) {
        element.SetValue(SourceProperty, value);
    }

    public static readonly AttachedProperty<bool> IsLoadingProperty =
        AvaloniaProperty.RegisterAttached<ImageBrush, bool>("IsLoading", typeof(ImageLoader));

    public static bool GetIsLoading(ImageBrush element) {
        return element.GetValue(IsLoadingProperty);
    }

    private static void SetIsLoading(ImageBrush element, bool value) {
        element.SetValue(IsLoadingProperty, value);
    }
}