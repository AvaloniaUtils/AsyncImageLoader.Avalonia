using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AsyncImageLoader.Avalonia.Demo.Pages {
    public partial class AdvancedImagePage : UserControl {
        public AdvancedImagePage() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        private void ReloadButton_OnClick(object? sender, RoutedEventArgs e) {
            var advancedImage = this.FindControl<AdvancedImage>("ReloadableAdvancedImage");
            advancedImage.Source = null;
            advancedImage.Source = "https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/raw/master/AsyncImageLoader.Avalonia.Demo/Assets/cat0.jpg";
        }
    }
}