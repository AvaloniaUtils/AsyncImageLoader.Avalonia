using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using AsyncImageLoader.Core.Leases;
using AsyncImageLoader.Core.Pipeline;
using AsyncImageLoader.Loaders;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.VisualTree;

namespace AsyncImageLoader;

public static class ImageLoader {
    private static readonly ParametrizedLogger? Logger;
    public const string AsyncImageLoaderLogArea = "AsyncImageLoader";

    public static readonly AttachedProperty<string?> SourceProperty =
        AvaloniaProperty.RegisterAttached<Image, string?>("Source", typeof(ImageLoader));

    public static readonly AttachedProperty<bool> IsLoadingProperty =
        AvaloniaProperty.RegisterAttached<Image, bool>("IsLoading", typeof(ImageLoader));

    private static readonly ConditionalWeakTable<Image, ImageState> States = new();

    static ImageLoader() {
        SourceProperty.Changed.AddClassHandler<Image>(OnSourceChanged);
        Logger = Avalonia.Logging.Logger.TryGet(LogEventLevel.Error, AsyncImageLoaderLogArea);
    }

    public static IAsyncImageLoader AsyncImageLoader { get; set; } = new RamCachedWebImageLoader();

    private static async void OnSourceChanged(Image sender, AvaloniaPropertyChangedEventArgs args) {
        var source = args.GetNewValue<string?>();
        var state = States.GetValue(sender, static _ => new ImageState());
        state.EnsureSubscribed(sender);

        if (!sender.IsAttachedToVisualTree()) {
            sender.Source = null;
            state.Coordinator.Cancel();
            SetIsLoading(sender, false);
            return;
        }

        await LoadAsync(sender, source, state);
    }

    private static void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs args) {
        if (sender is not Image image || !States.TryGetValue(image, out var state))
            return;

        _ = LoadAsync(image, GetSource(image), state);
    }

    private static void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs args) {
        if (sender is not Image image || !States.TryGetValue(image, out var state))
            return;

        image.Source = null;
        state.Coordinator.Cancel();
        SetIsLoading(image, false);
    }

    private static async Task LoadAsync(Image image, string? source, ImageState state) {
        SetIsLoading(image, !string.IsNullOrWhiteSpace(source));
        image.Source = null;
        var request = state.Coordinator.Begin();

        if (string.IsNullOrWhiteSpace(source)) {
            state.Coordinator.Cancel();
            return;
        }

        IImageLease? lease = null;
        try {
            await Task.Delay(10, request.CancellationToken);
            lease = await AsyncImageLoader.LoadAsync(new ImageLoadRequest(
                source,
                storageProvider: TopLevel.GetTopLevel(image)?.StorageProvider),
                request.CancellationToken);
        }
        catch (OperationCanceledException) when (request.CancellationToken.IsCancellationRequested) {
        }
        catch (Exception e) {
            Logger?.Log(LogEventLevel.Error, "ImageLoader image resolution failed: {0}", e);
        }

        if (!state.Coordinator.TrySetLease(request, lease))
            return;

        if (lease is not null)
            image.Source = lease.Image;

        if (state.Coordinator.TryComplete(request))
            SetIsLoading(image, false);
    }

    public static string? GetSource(Image element) => element.GetValue(SourceProperty);

    public static void SetSource(Image element, string? value) => element.SetValue(SourceProperty, value);

    public static bool GetIsLoading(Image element) => element.GetValue(IsLoadingProperty);

    private static void SetIsLoading(Image element, bool value) => element.SetValue(IsLoadingProperty, value);

    private sealed class ImageState {
        private int _subscribed;

        public ImageRequestCoordinator Coordinator { get; } = new();

        public void EnsureSubscribed(Image image) {
            if (Interlocked.Exchange(ref _subscribed, 1) != 0)
                return;

            image.AttachedToVisualTree += OnAttachedToVisualTree;
            image.DetachedFromVisualTree += OnDetachedFromVisualTree;
        }

        ~ImageState() {
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
