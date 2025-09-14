using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AsyncImageLoader.Avalonia.Demo.Pages;

public partial class AttachedPropertiesPage : UserControl {
    public AttachedPropertiesPage() {
        InitializeComponent();
    }

    private void InitializeComponent() {
        AvaloniaXamlLoader.Load(this);
    }
}