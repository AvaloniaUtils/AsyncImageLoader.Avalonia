using System.Collections.ObjectModel;

namespace AsyncImageLoader.Avalonia.Demo.ViewModels;

public class AdvancedImageSafeMemoryViewModel : ViewModelBase {
    public ObservableCollection<string> ImageUrls { get; } =
        new()
        {
            "https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/raw/master/AsyncImageLoader.Avalonia.Demo/Assets/cat0.jpg",
            "https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/raw/master/AsyncImageLoader.Avalonia.Demo/Assets/cat1.jpg",
            "https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/raw/master/AsyncImageLoader.Avalonia.Demo/Assets/cat2.jpg",
            "https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/raw/master/AsyncImageLoader.Avalonia.Demo/Assets/cat3.jpg",
            "https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/raw/master/AsyncImageLoader.Avalonia.Demo/Assets/cat4.jpg",
            "https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/raw/master/AsyncImageLoader.Avalonia.Demo/Assets/cat5.jpg",
            "https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/raw/master/AsyncImageLoader.Avalonia.Demo/Assets/cat6.jpg",
            "https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/raw/master/AsyncImageLoader.Avalonia.Demo/Assets/cat7.jpg",
        };
}