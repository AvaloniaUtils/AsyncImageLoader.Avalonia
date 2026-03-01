
using AsyncImageLoader.DevTools.Extensions;
using Avalonia;
using Avalonia.Controls;

namespace AsyncImageLoader.Avalonia.Demo.Views;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        
        this.AttachDevTools();
        this.AttachDevToolsBitmapInspector();
    }
}