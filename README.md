# **AsyncImageLoader.Avalonia**

Provides a way to **asynchronously load bitmaps** for Avalonia `Image` controls.

---

## **Features**

* Supports URLs and web downloading
* Asynchronous loading
* Integrated in-memory cache
* Integrated disk cache
* Easy to implement custom loading and caching

---

## **Getting started**

1. Install the `AsyncImageLoader.Avalonia` NuGet package:

```
dotnet add package AsyncImageLoader.Avalonia
```

2. Start using

---

## **Using**

> Note: The first time, you need to import the `AsyncImageLoader` namespace in your XAML file. Usually your IDE will suggest it automatically.

Example root element:

```xaml
<Window ...
        xmlns:asyncImageLoader="clr-namespace:AsyncImageLoader;assembly=AsyncImageLoader.Avalonia"
        ...>
   <!-- Your root element content -->
```

Avalonia assets documentation: [docs](https://docs.avaloniaui.net/docs/getting-started/assets)

---

### **ImageLoader attached property**

Replace the `Source` property of `Image` with `ImageLoader.Source`:

Old code:

```xaml
<Image Source="https://mycoolwebsite.io/image.jpg" />
```

New code:

```xaml
<Image asyncImageLoader:ImageLoader.Source="https://mycoolwebsite.io/image.jpg" />
```

You can also use the readonly `ImageLoader.IsLoading` property to check if loading is in progress.

**Supports `resm:` and `avares:` links**.
**Does not support relative assets** such as `Source="icon.png"` or `Source="/icon.png"`. Use the [AdvancedImage control](#advancedimage-control).

---

### **AdvancedImage control**

This control provides all the features of `ImageLoader.Source` and **supports relative assets**.

Add the style to your `App.xaml`:

```xaml
<StyleInclude Source="avares://AsyncImageLoader.Avalonia/AdvancedImage.axaml" />
```

Example usage:

```xaml
<asyncImageLoader:AdvancedImage Width="150" Height="150" Source="../Assets/cat4.jpg" />
```

* Allows specifying a custom `IAsyncImageLoader` per control
* Built-in support for loading indicators

---

### **ImageBrush**

If you need a brush, use `ImageBrushLoader.Source` instead of `Source`:

```xaml
<Border>
  <Border.Background>
    <ImageBrush
      asyncImageLoader:ImageBrushLoader.Source="https://mycoolwebsite.io/image.jpg" />
  </Border.Background>
</Border>
```

---

## **Loaders**

`ImageLoader` uses an instance of [IImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/IAsyncImageLoader.cs) to serve image requests.

You can change the loader by assigning a new instance to [ImageLoader.AsyncImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/ImageLoader.cs#L10).
**Remember to dispose the previous loader.**

---

### **Default loaders**

* [BaseWebImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/Loaders/BaseCachedWebImageLoader.cs) – loads images asynchronously **without caching**. Can be used as a base class if you want no caching.
* [RamCachedWebImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/Loaders/RamCachedWebImageLoader.cs) – inherits `BaseWebImageLoader` and adds **in-memory caching**.
* [DiskCachedWebImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/Loaders/DiskCachedWebImageLoader.cs) – inherits `RamCachedWebImageLoader` and adds **disk caching** for downloaded images.

### **New Smart Loaders**

* [SmartImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/Loaders/SmartImageLoader.cs) – inherits `BaseWebImageLoader` and implements **smart caching**:

  * Prevents simultaneous downloads of the same URL by reusing active tasks
  * Tracks usage references for each bitmap
  * Automatically evicts images unused for **20 seconds**
  * Reuses already loaded images without downloading again

* [SmartDiskImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/Loaders/SmartDiskImageLoader.cs) – inherits `SmartImageLoader` and adds **disk caching**

> Note: **automatic memory cleanup cannot be disabled anymore**. This is now the default behavior and only works for loaders inheriting from `SmartImageLoader`. Older loaders (`RamCachedWebImageLoader`, `DiskCachedWebImageLoader`) do not have this mechanism.