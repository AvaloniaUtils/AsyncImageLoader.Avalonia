using AsyncImageLoader.ViewModels;
using Avalonia.Controls;

namespace AsyncImageLoader.Views;

public partial class BitmapInspectorWindow : Window {
    public BitmapInspectorWindow() {
        InitializeComponent();
        DataContext = new BitmapInspectorDesignViewModel();
    }
}