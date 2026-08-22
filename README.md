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

## Image loading pipeline

`ImageLoaderPipeline` and `ImageLoaderPipelineBuilder` are the primary APIs for configuring image loading. The pipeline composes source resolution, external transport, encoded byte caching, bitmap decoding and decoded image retention. Start with the closest builder preset, then replace only the components your application needs to customize:

```csharp
using AsyncImageLoader.Core;

var loader = ImageLoaderPipelineBuilder.RamCached(new MemoryImageCacheOptions {
    MaxItems = 100,
    AbsoluteExpiration = TimeSpan.FromMinutes(10),
    SlidingExpiration = TimeSpan.FromMinutes(2)
})
    .UseHttpClient(new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
    .UseDecoder(new MyBitmapDecoder())
    .Build();

ImageLoader.AsyncImageLoader = loader;
```

The available presets are:

- `Uncached()` downloads and decodes each request without retaining the decoded image.
- `RamCached(...)` shares decoded images and retains them in the lease-aware RAM cache.
- `DiskCached(...)` adds a persistent encoded disk cache for HTTP and HTTPS sources.

All presets use the same default source resolvers, HTTP transport and bitmap decoder. They are starting configurations, not separate extension hierarchies.

Set the resulting pipeline globally through `ImageLoader.AsyncImageLoader` or `ImageBrushLoader.AsyncImageLoader`, or assign it to the `Loader` property of an individual `AdvancedImage`. Dispose the previous global loader when replacing it.

### Pipeline components

- `ImageLoadRequest` carries the source string and optional Avalonia context (`BaseUri` and `IStorageProvider`) through the pipeline.
- `IImageSourceResolver` handles non-network sources. The default `CompositeImageSourceResolver` tries `FileImageSourceResolver`, `StorageImageSourceResolver` and `AvaloniaAssetSourceResolver` in order.
- `IImageTransport` retrieves external encoded data. The default `HttpImageTransport` handles absolute HTTP and HTTPS sources using `HttpClient`.
- `IImageByteCache` stores encoded image data before decoding. `DiskImageByteCache` persists HTTP responses under hashed keys and is enabled by the `DiskCached(...)` preset.
- `IBitmapDecoder` converts an encoded stream into an Avalonia `Bitmap`. The default `BitmapDecoder` reads non-seekable streams asynchronously before constructing the bitmap.
- `IImageMemoryCache` coordinates concurrent requests and returns independent consumer leases. `TransientImageCache` performs no retention; `MemoryImageCache` provides RAM retention with absolute and sliding expiration.
- `IImageLease` represents one consumer's ownership of an image. UI integrations release their lease when a source is replaced or detached, while the memory cache controls how long its own reference is retained.
- `ImageLoaderPipeline` orchestrates these components and implements `IAsyncImageLoader`.

The builder methods replace individual components:

- `UseSourceResolver(...)`
- `UseTransport(...)`
- `UseDecoder(...)`
- `UseMemoryCache(...)`
- `UseByteCache(...)`
- `UseHttpClient(...)`

The built pipeline owns and disposes its configured memory cache. A supplied `HttpClient` remains caller-owned unless `UseHttpClient(client, disposeHttpClient: true)` is used. A builder can build only one pipeline because ownership of its cache is transferred during `Build()`.

### Compatibility loaders

The original ready-made loaders remain available as compatibility and convenience facades:

- [BaseWebImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/Loaders/BaseWebImageLoader.cs) corresponds to the `Uncached()` preset.
- [RamCachedWebImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/Loaders/RamCachedWebImageLoader.cs) corresponds to the `RamCached(...)` preset and remains the default global loader.
- [DiskCachedWebImageLoader](https://github.com/AvaloniaUtils/AsyncImageLoader.Avalonia/blob/master/AsyncImageLoader.Avalonia/Loaders/DiskCachedWebImageLoader.cs) corresponds to the `DiskCached(...)` preset.

These types delegate to the same pipeline presets. They are useful for existing applications and simple configurations, but new customization should use `ImageLoaderPipelineBuilder` instead of inheriting from a loader. On mobile, WASM and other restricted platforms, provide a valid writable cache path before using disk caching.

### Custom loaders

You can implement every component of the pipeline individually.

Or implement `IAsyncImageLoader` directly only when the complete built-in pipeline is not appropriate. `LoadAsync` receives an `ImageLoadRequest` and returns an `IImageLease`; such an implementation replaces source resolution, transport, decoding and caching rather than customizing one pipeline stage.

Use `ImageLease.Owned`, `ImageLease.NonOwning` or `ImageLease.Create` to make ownership explicit when implementing a custom loader.

### RAM retention

RAM retention can be configured when creating a loader. Expiration releases the loader's strong reference;
if the UI still uses the bitmap, it can be reused through a weak reference:

```csharp
ImageLoader.AsyncImageLoader = ImageLoaderPipelineBuilder.RamCached(new MemoryImageCacheOptions {
    AbsoluteExpiration = TimeSpan.FromMinutes(10),
    SlidingExpiration = TimeSpan.FromMinutes(2)
}).Build();
```

When both values are specified, the first expiration is used. Expiration never disposes bitmaps that have
already been returned to controls.
