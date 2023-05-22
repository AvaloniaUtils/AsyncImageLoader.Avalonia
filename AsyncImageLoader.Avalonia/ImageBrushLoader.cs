using System;
using System.Reactive.Linq;
using AsyncImageLoader.Loaders;
using Avalonia;
using Avalonia.Media;

namespace AsyncImageLoader {
    public static class ImageBrushLoader {
        public static IAsyncImageLoader AsyncImageLoader { get; set; } = new RamCachedWebImageLoader();
        static ImageBrushLoader() {
            SourceProperty.Changed
                .Where(args => args.IsEffectiveValueChange)
                .Subscribe(args => OnSourceChanged((ImageBrush)args.Sender, args.NewValue.Value));
        }

        private static async void OnSourceChanged(ImageBrush sender, string? url) {
            SetIsLoading(sender, true);

            var bitmap = url == null
                ? null
                : await AsyncImageLoader.ProvideImageAsync(url);
            if (GetSource(sender) != url) return;
            sender.Source = bitmap;

            SetIsLoading(sender, false);
        }

        public static readonly AttachedProperty<string?> SourceProperty = AvaloniaProperty.RegisterAttached<ImageBrush, string?>("Source", typeof(ImageLoader));

        public static string? GetSource(ImageBrush element) {
            return element.GetValue(SourceProperty);
        }

        public static void SetSource(ImageBrush element, string? value) {
            element.SetValue(SourceProperty, value);
        }

        public static readonly AttachedProperty<bool> IsLoadingProperty = AvaloniaProperty.RegisterAttached<ImageBrush, bool>("IsLoading", typeof(ImageLoader));

        public static bool GetIsLoading(ImageBrush element) {
            return element.GetValue(IsLoadingProperty);
        }

        private static void SetIsLoading(ImageBrush element, bool value) {
            element.SetValue(IsLoadingProperty, value);
        }
    }
}