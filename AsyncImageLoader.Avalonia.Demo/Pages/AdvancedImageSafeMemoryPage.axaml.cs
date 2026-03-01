using AsyncImageLoader.Avalonia.Demo.ViewModels;
using Avalonia.Controls;

namespace AsyncImageLoader.Avalonia.Demo.Pages;

public partial class AdvancedImageSafeMemoryPage : UserControl {
    public AdvancedImageSafeMemoryPage() {
        InitializeComponent();
        DataContext = new AdvancedImageSafeMemoryViewModel();
    }
}