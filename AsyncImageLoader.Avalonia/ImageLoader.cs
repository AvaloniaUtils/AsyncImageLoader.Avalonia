using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace AsyncImageLoader.Avalonia {
    public static class ImageLoader {
        public static IAsyncImageLoader AsyncImageLoader { get; set; } = new RamCachedWebImageLoader();
        static ImageLoader() {
            SourceUrlProperty.Changed.Subscribe(args => {
                _ = AsyncImageLoader.ProvideImageAsync(args.NewValue.Value)
                    .ContinueWith(task => {
                        var bitmap = task.GetAwaiter().GetResult();
                        if (GetSourceUrl((args.Sender as Image)!) != args.NewValue.Value) return;
                        Dispatcher.UIThread.Post(() => {
                            (args.Sender as Image)!.Source = bitmap;
                        });
                    }, TaskContinuationOptions.OnlyOnRanToCompletion);
            });
        }

        public static readonly AttachedProperty<string> SourceUrlProperty = AvaloniaProperty.RegisterAttached<Image, string>("SourceUrl", typeof(ImageLoader));

        public static string GetSourceUrl(Image element) {
            return element.GetValue(SourceUrlProperty);
        }

        public static void SetSourceUrl(Image element, string value) {
            element.SetValue(SourceUrlProperty, value);
        }
    }
}