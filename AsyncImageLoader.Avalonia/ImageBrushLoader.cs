using System;
using System.Runtime.CompilerServices;
using System.Threading;
using AsyncImageLoader.Core;
using AsyncImageLoader.Core.Leases;
using AsyncImageLoader.Core.Pipeline;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader;

public static class ImageBrushLoader {
    private static readonly ParametrizedLogger? Logger;
    public static IAsyncImageLoader AsyncImageLoader { get; set; } =
        ImageLoaderPipelineBuilder.RamCached().Build();
    private static readonly ConditionalWeakTable<ImageBrush, BrushState> States = new();

    static ImageBrushLoader() {
        SourceProperty.Changed.AddClassHandler<ImageBrush>(OnSourceChanged);
        Logger = Avalonia.Logging.Logger.TryGet(LogEventLevel.Error, ImageLoader.AsyncImageLoaderLogArea);
    }

    private static async void OnSourceChanged(ImageBrush imageBrush, AvaloniaPropertyChangedEventArgs args) {
        var (oldValue, newValue) = args.GetOldAndNewValue<string?>();
        if (oldValue == newValue)
            return;

        var state = States.GetValue(imageBrush, static _ => new BrushState());
        SetIsLoading(imageBrush, true);
        imageBrush.Source = null;
        var request = state.Coordinator.Begin();

        IImageBrushSource? image = null;
        IImageLease? lease = null;
        try {
            if (!string.IsNullOrWhiteSpace(newValue)) {
                lease = await AsyncImageLoader.LoadAsync(new ImageLoadRequest(newValue), request.CancellationToken);
                image = lease?.Image as IImageBrushSource;
                if (lease is not null && image is null) {
                    lease.Dispose();
                    lease = null;
                }
            }

            if (image == null && GetFallbackImage(imageBrush) is { } fallback)
                image = fallback;
        }
        catch (OperationCanceledException) when (request.CancellationToken.IsCancellationRequested) {
        }
        catch (Exception e) {
            Logger?.Log("ImageBrushLoader", "ImageBrushLoader image resolution failed: {0}", e);
        }

        if (state.Coordinator.TrySetLease(request, lease)) {
            if (GetSource(imageBrush) == newValue) {
                imageBrush.Source = image;
            }

            if (state.Coordinator.TryComplete(request))
                SetIsLoading(imageBrush, false);
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

    private sealed class BrushState {
        public ImageRequestCoordinator Coordinator { get; } = new();

        ~BrushState() {
            var coordinator = Coordinator;
            ThreadPool.QueueUserWorkItem(static state => {
                try {
                    ((ImageRequestCoordinator)state!).Dispose();
                }
                catch {
                    // A custom release callback must not escape a GC fallback.
                }
            }, coordinator);
        }
    }
}
