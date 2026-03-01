using Avalonia.Controls;
using Avalonia.Input;

namespace AsyncImageLoader.DevTools.Extensions;

public static class DevToolsBitmapInspector {
    public static void AttachDevToolsBitmapInspector(this TopLevel root) 
        => DevTools.DevToolsBitmapInspector.Attach(root, new KeyGesture(Key.I, KeyModifiers.Control));
    
    public static void AttachDevToolsBitmapInspector(this TopLevel root, KeyGesture gesture) 
        => DevTools.DevToolsBitmapInspector.Attach(root, gesture);
}