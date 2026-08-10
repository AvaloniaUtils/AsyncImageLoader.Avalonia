using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using AsyncImageLoader.Core;
using AsyncImageLoader.Loaders;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader;

public static class ImageBrushLoader {
    private static readonly ParametrizedLogger? Logger;
    public static IAsyncImageLoader AsyncImageLoader { get; set; } = new RamCachedWebImageLoader();
    private static readonly ConcurrentDictionary<ImageBrush, PendingOperation> PendingOperations = new();

    static ImageBrushLoader() {
        SourceProperty.Changed.AddClassHandler<ImageBrush>(OnSourceChanged);
        Logger = Avalonia.Logging.Logger.TryGet(LogEventLevel.Error, ImageLoader.AsyncImageLoaderLogArea);
    }

    private static async void OnSourceChanged(ImageBrush imageBrush, AvaloniaPropertyChangedEventArgs args) {
        var (oldValue, newValue) = args.GetOldAndNewValue<string?>();
        if (oldValue == newValue)
            return;

        var operation = PendingOperations.AddOrUpdate(imageBrush, new PendingOperation(),
            (_, old) => {
                old.Dispose();
                return new PendingOperation();
            });
        SetIsLoading(imageBrush, true);
        imageBrush.Source = null;

        Bitmap? bitmap = null;
        try {
            if (!string.IsNullOrWhiteSpace(newValue)) {
                operation.Lease = await AsyncImageLoader.LoadAsync(new ImageLoadRequest(newValue!), operation.Cancellation.Token);
                bitmap = operation.Lease?.Image as Bitmap;
            }

            if (bitmap == null && GetFallbackImage(imageBrush) is Bitmap fallback)
                bitmap = fallback;
        }
        catch (Exception e) {
            Logger?.Log("ImageBrushLoader", "ImageBrushLoader image resolution failed: {0}", e);
        }

        if (PendingOperations.TryRemove(new KeyValuePair<ImageBrush, PendingOperation>(imageBrush, operation))) {
            if (GetSource(imageBrush) == newValue) {
                imageBrush.Source = bitmap;
            }

            SetIsLoading(imageBrush, false);
            operation.DisposeCancellation();
        }
    }

    public static readonly AttachedProperty<string?> SourceProperty =
        AvaloniaProperty.RegisterAttached<ImageBrush, string?>("Source", typeof(ImageLoader));

    /// <summary>
    /// Attached property that provides a fallback <see cref="Bitmap"/> to use when <see cref="SourceProperty"/> is null or empty.
    /// </summary>
    public static readonly AttachedProperty<Bitmap?> FallbackImageProperty =
        AvaloniaProperty.RegisterAttached<ImageBrush, Bitmap?>("FallbackImage", typeof(Bitmap));

    /// <summary>
    /// Gets the fallback <see cref="Bitmap"/> attached to the specified <see cref="ImageBrush"/>.
    /// Returns <c>null</c> if no fallback image has been set.
    /// </summary>
    /// <param name="element">The <see cref="ImageBrush"/> to read the fallback image from.</param>
    /// <returns>The fallback <see cref="Bitmap"/>, or <c>null</c> if none is set.</returns>
    public static Bitmap? GetFallbackImage(ImageBrush element) {
        return element.GetValue(FallbackImageProperty);
    }

    /// <summary>
    /// Sets the fallback <see cref="Bitmap"/> on the specified <see cref="ImageBrush"/>.
    /// The fallback image is used when the <see cref="SourceProperty"/> value is null or empty.
    /// </summary>
    /// <param name="element">The <see cref="ImageBrush"/> to set the fallback image on.</param>
    /// <param name="value">The <see cref="Bitmap"/> to use as the fallback</param>
    public static void SetFallbackImage(ImageBrush element, Bitmap? value) {
        element.SetValue(FallbackImageProperty, value);
    }

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

    private sealed class PendingOperation : IDisposable {
        public CancellationTokenSource Cancellation { get; } = new();
        public IImageLease? Lease { get; set; }

        public void Dispose() {
            Cancellation.Cancel();
            Lease?.Dispose();
            Cancellation.Dispose();
        }

        public void DisposeCancellation() {
            Cancellation.Dispose();
        }
    }
}
