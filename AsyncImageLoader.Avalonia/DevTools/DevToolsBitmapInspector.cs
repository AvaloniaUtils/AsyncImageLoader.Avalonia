using System;
using AsyncImageLoader.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AsyncImageLoader.DevTools;

internal static class DevToolsBitmapInspector 
{
    private static BitmapInspectorWindow? _window;
    
    public static IDisposable Attach(TopLevel root, KeyGesture gesture)
    {
        void PreviewKeyDown(object? sender, KeyEventArgs e) {
            if (gesture.Matches(e)) 
            {
                Open(root);
            }
        }
        
        return (root ?? throw new ArgumentNullException(nameof(root))).AddDisposableHandler(
            InputElement.KeyDownEvent,
            PreviewKeyDown,
            RoutingStrategies.Tunnel);
    }
    
    public static void Open(TopLevel root)
    {
        if (_window == null)
        {
            _window = new BitmapInspectorWindow();

            _window.Closed += (_, _) =>
            {
                _window = null;
            };

            _window.Opened += (_, _) => 
            {
                PositionWindow(root);
            };
        }

        if (!_window.IsVisible)
        {
            if (root is Window owner)
                _window.Show(owner);
            else
                _window.Show();
        }

        if (_window.WindowState == WindowState.Minimized)
            _window.WindowState = WindowState.Normal;

        _window.Activate();
    }
    
    private static void PositionWindow(TopLevel root)
    {
        if (_window == null)
            return;

        var screen = _window.Screens.ScreenFromVisual(root);

        if (screen == null)
            return;

        var area = screen.WorkingArea;
        var bounds = _window.Bounds;

        _window.Position = new PixelPoint(
            area.Right - (int)bounds.Width,
            area.Bottom - (int)bounds.Height);
    }

}