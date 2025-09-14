using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AsyncImageLoader.Avalonia.Demo.Pages;

public partial class AdvancedImagePage : UserControl {
    public AdvancedImagePage() {
        InitializeComponent();
    }

    private void ReloadButton_OnClick(object? sender, RoutedEventArgs e) {
        ReloadableAdvancedImage.Source = null;
        ReloadableAdvancedImage.Source =
            "https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/raw/master/AsyncImageLoader.Avalonia.Demo/Assets/cat0.jpg";
    }

    private void SetSourceButton_OnClick(object? sender, RoutedEventArgs e) {
        CurrentImageExample.Source = "/Assets/cat5.jpg";
    }

    private void SetCurrentImageButton_OnClick(object? sender, RoutedEventArgs e) {
        using var stream = AssetLoader.Open(new Uri("avares://AsyncImageLoader.Avalonia.Demo/Assets/cat4.jpg",
            UriKind.RelativeOrAbsolute));
        CurrentImageExample.CurrentImage = new Bitmap(stream);
    }
}