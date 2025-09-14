# AsyncImageLoader.Avalonia

Provides way to asynchronous bitmap loading for Avalonia Image control.  
Features:
- Supports urls and downloading from web
- Asynchronous loading
- Integrated inmemory cache
- Integrated disk cache
- Easy to implement your own way of images loading and caching

## Getting started

1. Install `AsyncImageLoader.Avalonia` [nuget package](https://www.nuget.org/packages/AsyncImageLoader.Avalonia/)
```
dotnet add package AsyncImageLoader.Avalonia
```
2. Start using

## Using

Note: The first time you will need to import the AsyncImageLoader namespace to your xaml file. Usually your IDE should [suggest it automatically](https://user-images.githubusercontent.com/29896317/140953397-00028365-5b93-4e6c-b470-094a555870c8.png). The root element in the file will be [like this](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia.Demo/Views/MainWindow.axaml#L6):
```xaml
<Window ...
        xmlns:asyncImageLoader="clr-namespace:AsyncImageLoader;assembly=AsyncImageLoader.Avalonia"
        ...>
   <!-- Your root element content -->
```
Note: Assets and resources in Avalonia described [here](https://docs.avaloniaui.net/docs/getting-started/assets).

### ImageLoader attached property
The only thing you need to do in your xaml is to replace the `Source` property in `Image` with `ImageLoader.Source`.  
For example, your old code:  
```xaml
<Image Source="https://mycoolwebsite.io/image.jpg" />
``` 
Should turn into:
```xaml
<Image asyncImageLoader:ImageLoader.Source="https://mycoolwebsite.io/image.jpg" />
```
Also you can use `ImageLoader.IsLoading` readonly attached property that indicates whether the load is in progress or not.

AsyncImageLoader **support** `resm:` and `avares:` links.
And does **not** support relative referenced assets such as `Source="icon.png"` or `Source="/icon.png"`. Use [AdvancedImage control](#advancedimage-control).

### AdvancedImage control
This control provides all capabilities of ImageLoader attached property and **support** relative referenced assets such as `Source="icon.png"` or `Source="/icon.png"`.
Before you go, add following style to you `App.xaml` file and `Application.Styles` section:
```xaml
<StyleInclude Source="avares://AsyncImageLoader.Avalonia/AdvancedImage.axaml" />
```
And you can use `AdvancedImage` as any other control:
```xaml
<asyncImageLoader:AdvancedImage Width="150" Height="150" Source="../Assets/cat4.jpg" />
```
This control allows specifying a custom IAsyncImageLoader for particular control.  
Also, this control has loading indicator support out of the box.

### ImageBrush
If you need a brush you can use Avalonia's `ImageBrush` with `ImageBrushLoader.Source` property (instead of default `Source`). It will look like that:
```xaml
<Border>
  <Border.Background>
    <ImageBrush
      asyncImageLoader:ImageBrushLoader.Source="https://mycoolwebsite.io/image.jpg" />
  </Border.Background>
</Border>
```

## Loaders
ImageLoader will use instance of [IImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/IAsyncImageLoader.cs) for serving your requests.  
You can change the loader used by setting new one to the [ImageLoader.AsyncImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/ImageLoader.cs#L10) property. Do not forget to Dispose previous loader.  
There are several loaders available out of the box: 
- [BaseWebImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/Loaders/BaseCachedWebImageLoader.cs) - Provides non cached way to asynchronously load images without caching. Can be used as base class for custom loaders you dont want caching in any way.
- [RamCachedWebImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/Loaders/RamCachedWebImageLoader.cs) - This is inheritor if BaseWebImageLoader with in memory images caching. Can be used as base class for custom loaders you want only inmemory caching.
- [DiskCachedWebImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/Loaders/DiskCachedWebImageLoader.cs) - This is inheritor if RamCachedWebImageLoader with in memory caching and disk caching for downloaded from the internet images. Can be used as base class for custom loaders if you want disk caching out of the box.  
  If you are using DiskCachedWebImageLoader on a non-PC platforms (mobile/wasm/etc) make sure to specify correct path for storing files on this platform. Default most likely doesn't work there.

`RamCachedWebImageLoader` are used by default.
