using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace AsyncImageLoader.Avalonia {
    public static class ImageLoader {
        public static IAsyncImageLoader AsyncImageLoader { get; set; } = new RamCachedWebImageLoader();
        static ImageLoader() {
            SourceProperty.Changed.Subscribe(args => {
                _ = AsyncImageLoader.ProvideImageAsync(args.NewValue.Value)
                    .ContinueWith(task => {
                        var bitmap = task.GetAwaiter().GetResult();
                        var image = (args.Sender as Image)!;
                        Dispatcher.UIThread.Post(() => {
                            if (GetSource(image) != args.NewValue.Value) return;
                            image.Source = bitmap;
                        });
                    }, TaskContinuationOptions.OnlyOnRanToCompletion);
            });
        }

        public static readonly AttachedProperty<string> SourceProperty = AvaloniaProperty.RegisterAttached<Image, string>("Source", typeof(ImageLoader));

        public static string GetSource(Image element) {
            return element.GetValue(SourceProperty);
        }

        public static void SetSource(Image element, string value) {
            element.SetValue(SourceProperty, value);
        }
    }
}