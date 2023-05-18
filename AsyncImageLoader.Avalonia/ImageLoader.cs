using System;
using AsyncImageLoader.Loaders;
using Avalonia;
using Avalonia.Controls;

namespace AsyncImageLoader
{
    public static class ImageLoader
    {
        public const string AsyncImageLoaderLogArea = "AsyncImageLoader";

        public static readonly AttachedProperty<string?> SourceProperty =
            AvaloniaProperty.RegisterAttached<Image, string?>("Source", typeof(ImageLoader));

        public static readonly AttachedProperty<bool> IsLoadingProperty =
            AvaloniaProperty.RegisterAttached<Image, bool>("IsLoading", typeof(ImageLoader));

        static ImageLoader()
        {
            SourceProperty.Changed.AddClassHandler(new Action<Image,AvaloniaPropertyChangedEventArgs<string?>>(
                (image, args) =>
                {
                    OnSourceChanged(image, args.NewValue.Value);
                }));
            /*
            SourceProperty.Changed
                .Subscribe(args => OnSourceChanged((Image)args.Sender, args.NewValue.Value));
            */
        }

        public static IAsyncImageLoader AsyncImageLoader { get; set; } = new RamCachedWebImageLoader();

        private static async void OnSourceChanged(Image sender, string? url)
        {
            SetIsLoading(sender, true);

            var bitmap = url == null
                ? null
                : await AsyncImageLoader.ProvideImageAsync(url);
            if (GetSource(sender) != url) return;
            sender.Source = bitmap!;

            SetIsLoading(sender, false);
        }

        public static string? GetSource(Image element)
        {
            return element.GetValue(SourceProperty);
        }

        public static void SetSource(Image element, string? value)
        {
            element.SetValue(SourceProperty, value);
        }

        public static bool GetIsLoading(Image element)
        {
            return element.GetValue(IsLoadingProperty);
        }

        private static void SetIsLoading(Image element, bool value)
        {
            element.SetValue(IsLoadingProperty, value);
        }
    }
}