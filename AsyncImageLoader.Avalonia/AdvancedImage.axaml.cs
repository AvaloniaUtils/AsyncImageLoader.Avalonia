using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Memory.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AsyncImageLoader;

public class AdvancedImage : ContentControl {
    /// <summary>
    ///     Defines the <see cref="Loader" /> property.
    /// </summary>
    public static readonly StyledProperty<IAsyncImageLoader?> LoaderProperty =
        AvaloniaProperty.Register<AdvancedImage, IAsyncImageLoader?>(nameof(Loader));

    /// <summary>
    ///     Defines the <see cref="AutoCleanupEnabled" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> AutoCleanupEnabledProperty =
        AvaloniaProperty.Register<AdvancedImage, bool>(nameof(AutoCleanupEnabled));

    /// <summary>
    ///     Defines the <see cref="Source" /> property.
    /// </summary>
    public static readonly StyledProperty<string?> SourceProperty =
        AvaloniaProperty.Register<AdvancedImage, string?>(nameof(Source));
    
    /// <summary>
    ///     Defines the <see cref="FallbackImage" /> property.
    /// </summary>
    public static readonly StyledProperty<Bitmap?> FallbackImageProperty =
        AvaloniaProperty.Register<AdvancedImage, Bitmap?>(nameof(FallbackImage));

    /// <summary>
    ///     Defines the <see cref="ShouldLoaderChangeTriggerUpdate" /> property.
    /// </summary>
    public static readonly DirectProperty<AdvancedImage, bool> ShouldLoaderChangeTriggerUpdateProperty =
        AvaloniaProperty.RegisterDirect<AdvancedImage, bool>(
            nameof(ShouldLoaderChangeTriggerUpdate),
            image => image._shouldLoaderChangeTriggerUpdate,
            (image, b) => image._shouldLoaderChangeTriggerUpdate = b
        );

    /// <summary>
    ///     Defines the <see cref="IsLoading" /> property.
    /// </summary>
    public static readonly DirectProperty<AdvancedImage, bool> IsLoadingProperty =
        AvaloniaProperty.RegisterDirect<AdvancedImage, bool>(
            nameof(IsLoading),
            image => image._isLoading);

    /// <summary>
    ///     Defines the <see cref="CurrentImage" /> property.
    /// </summary>
    public static readonly DirectProperty<AdvancedImage, IImage?> CurrentImageProperty =
        AvaloniaProperty.RegisterDirect<AdvancedImage, IImage?>(
            nameof(CurrentImage),
            image => image._currentImage);

    /// <summary>
    ///     Defines the <see cref="Stretch" /> property.
    /// </summary>
    public static readonly StyledProperty<Stretch> StretchProperty =
        Image.StretchProperty.AddOwner<AdvancedImage>();

    /// <summary>
    ///     Defines the <see cref="StretchDirection" /> property.
    /// </summary>
    public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
        Image.StretchDirectionProperty.AddOwner<AdvancedImage>();
    
    private readonly Uri? _baseUri;

    private RoundedRect _cornerRadiusClip;

    private IImage? _currentImage;
    private bool _isCornerRadiusUsed;

    private bool _isLoading;
    
    private bool _shouldLoaderChangeTriggerUpdate;
    
    private CancellationTokenSource? _updateCancellationToken;
    private readonly ParametrizedLogger? _logger;
    
    private BitmapLease? _lease;
    private bool _isInsideVirtualizingPanel;

    static AdvancedImage() {
        AffectsRender<AdvancedImage>(CurrentImageProperty, StretchProperty, StretchDirectionProperty,
            CornerRadiusProperty);
        AffectsMeasure<AdvancedImage>(CurrentImageProperty, StretchProperty, StretchDirectionProperty);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AdvancedImage" /> class.
    /// </summary>
    /// <param name="baseUri">The base URL for the XAML context.</param>
    public AdvancedImage(Uri? baseUri) {
        _baseUri = baseUri;
        _logger = Logger.TryGet(LogEventLevel.Error, ImageLoader.AsyncImageLoaderLogArea);
        _isInsideVirtualizingPanel = IsInsideVirtualizingPanel(this);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AdvancedImage" /> class.
    /// </summary>
    /// <param name="serviceProvider">The XAML service provider.</param>
    public AdvancedImage(IServiceProvider serviceProvider)
        : this((serviceProvider.GetService(typeof(IUriContext)) as IUriContext)?.BaseUri) {
    }

    /// <summary>
    /// Gets or sets a value indicating whether automatic cleanup of image resources is enabled.
    /// When enabled, the control may automatically release cached or unused image resources
    /// to help reduce memory usage. When disabled, resource cleanup must be handled manually
    /// or by other mechanisms.
    /// </summary>
    public bool AutoCleanupEnabled
    {
        get => GetValue(AutoCleanupEnabledProperty);
        set => SetValue(AutoCleanupEnabledProperty, value);
    }

    /// <summary>
    ///     Gets or sets the URI for image that will be displayed.
    /// </summary>
    public IAsyncImageLoader? Loader {
        get => GetValue(LoaderProperty);
        set => SetValue(LoaderProperty, value);
    }

    /// <summary>
    ///     Gets or sets the URI for image that will be displayed.
    /// </summary>
    public string? Source {
        get => GetValue(SourceProperty);
        set 
        {
            SetValue(SourceProperty, value);
        }
    }

    /// <summary>
    ///     Gets or sets the value controlling whether the image should be reloaded after changing the loader.
    /// </summary>
    public bool ShouldLoaderChangeTriggerUpdate {
        get => _shouldLoaderChangeTriggerUpdate;
        set => SetAndRaise(ShouldLoaderChangeTriggerUpdateProperty, ref _shouldLoaderChangeTriggerUpdate, value);
    }
    
    /// <summary>
    ///     Gets or sets the Bitmap for Fallback image that will be displayed if the Source image isn't loaded.
    /// </summary>
    public Bitmap? FallbackImage {
        get => GetValue(FallbackImageProperty);
        set => SetValue(FallbackImageProperty, value);
    }

    /// <summary>
    ///     Gets a value indicating is image currently is loading state.
    /// </summary>
    public bool IsLoading {
        get => _isLoading;
        private set => SetAndRaise(IsLoadingProperty, ref _isLoading, value);
    }

    /// <summary>
    ///     Gets a currently loaded IImage.
    /// </summary>
    public IImage? CurrentImage {
        get => _currentImage;
        set => SetAndRaise(CurrentImageProperty, ref _currentImage, value);
    }

    /// <summary>
    ///     Gets or sets a value controlling how the image will be stretched.
    /// </summary>
    public Stretch Stretch {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value controlling in what direction the image will be stretched.
    /// </summary>
    public StretchDirection StretchDirection {
        get => GetValue(StretchDirectionProperty);
        set => SetValue(StretchDirectionProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
        if (change.Property == SourceProperty)
            _ = UpdateImage(change.GetNewValue<string>(), Loader);
        else if (change.Property == LoaderProperty && ShouldLoaderChangeTriggerUpdate)
            _ = UpdateImage(change.GetNewValue<string>(), Loader);
        else if (change.Property == CurrentImageProperty)
            ClearSourceIfUserProvideImage();
        else if (change.Property == CornerRadiusProperty)
            UpdateCornerRadius(change.GetNewValue<CornerRadius>());
        else if (change.Property == BoundsProperty && CornerRadius != default) UpdateCornerRadius(CornerRadius);
        else if (change.Property == FallbackImageProperty && Source == null)
            _ = UpdateImage(null, null);
        
        base.OnPropertyChanged(change);
    }

    private void ClearSourceIfUserProvideImage() {
        if (CurrentImage is not null and not ImageWrapper) {
            // User provided image himself
            Source = null;
        }
    }

    private async Task UpdateImage(string? source, IAsyncImageLoader? loader) {
        var cts = ReplaceCts(ref _updateCancellationToken);
        
        if (source is null && FallbackImage != null)
            CurrentImage =  FallbackImage;

        if (source is null && CurrentImage is not ImageWrapper)
            return;

        IsLoading = true;
        
        if (CurrentImage is ImageWrapper wrapper)
            wrapper.Dispose();

        CurrentImage = null;

        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;

        BitmapLease? lease;

        try
        {
            lease = await LoadImageInternalAsync(source, loader, storage, cts.Token);
        }
        finally
        {
            cts.Cancel();
            cts.Dispose();
        }
        
        CurrentImage = lease is null ? null : new ImageWrapper(lease);
        IsLoading = false;
    }


    
    private async Task<BitmapLease?> LoadImageInternalAsync(
        string? source,
        IAsyncImageLoader? loader,
        IStorageProvider? storage,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        loader ??= ImageLoader.AsyncImageLoader;

        if (source == null)
            return null;
        
        BitmapLease? lease = null;

        try {
            var entry = await Load(source, loader, token);

            if (entry is null)
                return null;

            lease = new BitmapLease(entry);

            token.ThrowIfCancellationRequested();
            
            return lease;
        }
        catch (TaskCanceledException)
        {
            lease?.Dispose();
            throw;
        }
        catch
        {
            lease?.Dispose();
            throw;
        }
    }
    
    private async Task<BitmapEntry?> Load(string source, IAsyncImageLoader loader, CancellationToken token) {
        Loader ??= ImageLoader.AsyncImageLoader;
        
        token.ThrowIfCancellationRequested();

        var uri = new Uri(source, UriKind.RelativeOrAbsolute);
        
        if (AssetLoader.Exists(uri, _baseUri))
        {
            if(Loader is ICoordinatedImageLoader && BitmapStore.Instance.TryGet(source, out var entry))
                return entry;
            
            token.ThrowIfCancellationRequested();
            
            using var stream = AssetLoader.Open(uri, _baseUri);
            
            entry = new BitmapEntry(source, Bitmap.DecodeToWidth(stream, (int)Width));
            
            BitmapStore.Instance.TryAdd(entry);

            return entry;
        }

        token.ThrowIfCancellationRequested();

        if (Loader is ICoordinatedImageLoader coordinated) {
            var entry = await coordinated.CoordinatorProvideImageAsync(source);
                
            if(entry == null)
                return null;

            return entry;
        }
        
        var bitmap = await loader
            .ProvideImageAsync(source)
            .ConfigureAwait(false);

        if (bitmap == null)
            return null;
        
        return new BitmapEntry(source, bitmap);
    }
    
    private void UpdateCornerRadius(CornerRadius radius) {
        _isCornerRadiusUsed = radius != default;
        _cornerRadiusClip = new RoundedRect(new Rect(0, 0, Bounds.Width, Bounds.Height), radius);
    }

    /// <summary>
    ///     Renders the control.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    public override void Render(DrawingContext context) {
        var source = CurrentImage;

        if (source != null && Bounds is { Width: > 0, Height: > 0 }) {
            var viewPort = new Rect(Bounds.Size);
            var sourceSize = source.Size;

            var scale = Stretch.CalculateScaling(Bounds.Size, sourceSize, StretchDirection);
            var scaledSize = sourceSize * scale;
            var destRect = viewPort
                .CenterRect(new Rect(scaledSize))
                .Intersect(viewPort);
            var sourceRect = new Rect(sourceSize)
                .CenterRect(new Rect(destRect.Size / scale));

            DrawingContext.PushedState? pushedState =
                _isCornerRadiusUsed ? context.PushClip(_cornerRadiusClip) : null;
            context.DrawImage(source, sourceRect, destRect);
            pushedState?.Dispose();
        }
        else {
            base.Render(context);
        }
    }

    /// <summary>
    ///     Measures the control.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size of the control.</returns>
    protected override Size MeasureOverride(Size availableSize) {
        return CurrentImage != null
            ? Stretch.CalculateSize(availableSize, CurrentImage.Size, StretchDirection)
            : base.MeasureOverride(availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize) {
        return CurrentImage != null
            ? Stretch.CalculateSize(finalSize, CurrentImage.Size)
            : base.ArrangeOverride(finalSize);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
        if(!_isInsideVirtualizingPanel)
            AcquireImage();
        base.OnAttachedToVisualTree(e);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
        if(!_isInsideVirtualizingPanel)
            ReleaseImage();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnDataContextChanged(EventArgs e) {
        if(_isInsideVirtualizingPanel)
            ReleaseImage();
        
        base.OnDataContextChanged(e);
    }

    private void AcquireImage() 
    {
        if(!AutoCleanupEnabled)
            return;
        
        if (Loader == null)
            Loader = ImageLoader.AsyncImageLoader;
        
        var loader = Loader;

        if (loader == null || loader is not ICoordinatedImageLoader)
            return;

        if (Loader is ICoordinatedImageLoader)
            if (CurrentImage is null) 
                _ = Dispatcher.UIThread.InvokeAsync(async () => await UpdateImage(Source, Loader));
    }
    
    private void ReleaseImage()
    {
        if(!AutoCleanupEnabled)
            return;
        
        var loader = Loader;

        if (loader == null || loader is not ICoordinatedImageLoader)
            return;
        
        if (CurrentImage is ImageWrapper wrapper)
        {
            wrapper.Dispose();
            CurrentImage = null;
        }
    }
    
    private static bool IsInsideVirtualizingPanel(AdvancedImage visual)
    {
        var parent = visual.GetVisualParent();

        while (parent != null)
        {
            if (parent is VirtualizingPanel)
                return true;

            parent = parent.GetVisualParent();
        }

        return false;
    }
    
    private static CancellationTokenSource ReplaceCts(ref CancellationTokenSource? field)
    {
        var newCts = new CancellationTokenSource();
        var old = Interlocked.Exchange(ref field, newCts);

        if (old != null)
        {
            try { old.Cancel(); }
            catch { }
            old.Dispose();
        }

        return newCts;
    }

    public sealed class ImageWrapper : IImage, IDisposable
    {
        private BitmapLease? _lease;
        private int _disposed;

        private readonly Size _size;

        public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

        public ImageWrapper(BitmapLease lease)
        {
            _lease = lease;

            var bmp = lease.Bitmap 
                      ?? throw new ObjectDisposedException(nameof(BitmapLease));

            _size = new Size(bmp.Size.Width, bmp.Size.Height);
        }

        ~ImageWrapper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            if (disposing)
            {
                _lease?.Dispose();
            }

            _lease = null;
        }

        public Size Size => _size;

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
            if (IsDisposed)
                return;

            var lease = _lease;
            var bmp = lease?.Bitmap;

            if (bmp == null)
                return;

            ((IImage)bmp).Draw(context, sourceRect, destRect);
        }
    }
}