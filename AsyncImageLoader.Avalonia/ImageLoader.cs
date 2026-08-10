using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using AsyncImageLoader.Core;
using AsyncImageLoader.Loaders;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System.Collections.Concurrent;
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

    private static readonly ConcurrentDictionary<Image, PendingOperation> PendingOperations = new();

    private static async void OnSourceChanged(Image sender, AvaloniaPropertyChangedEventArgs args) {
        var url = args.GetNewValue<string?>();

        // Cancel/Add new pending operation
        var operation = PendingOperations.AddOrUpdate(sender, new PendingOperation(),
            (x, y) => {
                y.Dispose();
                return new PendingOperation();
            });
        var cts = operation.Cancellation;

        if (string.IsNullOrWhiteSpace(url)) {
            PendingOperations.TryRemove(new KeyValuePair<Image, PendingOperation>(sender, operation));
            operation.Dispose();
            sender.Source = null;
            SetIsLoading(sender, false);
            return;
        }

        SetIsLoading(sender, true);
        sender.Source = null;

        IImageLease? lease = null;
        try {
            try {
                await Task.Delay(10, cts.Token);

                lease = await AsyncImageLoader.LoadAsync(new ImageLoadRequest(
                    url,
                    storageProvider: TopLevel.GetTopLevel(sender)?.StorageProvider),
                    cts.Token);
            }
            catch (TaskCanceledException) {
            }
            catch (Exception e) {
                Logger?.Log(LogEventLevel.Error, "ImageLoader image resolution failed: {0}", e);
            }
        }
        finally {
            if (PendingOperations.TryRemove(new KeyValuePair<Image, PendingOperation>(sender, operation))) {
                if (lease is not null && !cts.IsCancellationRequested) {
                    operation.Lease = lease;
                    sender.Source = lease.Image as Bitmap;
                }
                else {
                    lease?.Dispose();
                }

                operation.DisposeCancellation();
                SetIsLoading(sender, false);
            }
            else {
                lease?.Dispose();
            }
        }
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
