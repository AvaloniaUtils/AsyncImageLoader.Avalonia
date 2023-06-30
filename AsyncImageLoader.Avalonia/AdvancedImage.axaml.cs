using System;
using System.Reactive.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AsyncImageLoader
{
    public class AdvancedImage : ContentControl
    {

        /// <summary>
        /// Defines the <see cref="Loader"/> property.
        /// </summary>
        public static readonly StyledProperty<IAsyncImageLoader?> LoaderProperty =
            AvaloniaProperty.Register<AdvancedImage, IAsyncImageLoader?>(nameof(Loader));

        /// <summary>
        /// Defines the <see cref="Source"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> SourceProperty =
            AvaloniaProperty.Register<AdvancedImage, string?>(nameof(Source));

        /// <summary>
        /// Defines the <see cref="ShouldLoaderChangeTriggerUpdate"/> property.
        /// </summary>
        public static readonly DirectProperty<AdvancedImage, bool> ShouldLoaderChangeTriggerUpdateProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImage, bool>(
                                                                 nameof(ShouldLoaderChangeTriggerUpdate),
                                                                 image => image._shouldLoaderChangeTriggerUpdate,
                                                                 (image, b) => image._shouldLoaderChangeTriggerUpdate = b
                                                                );

        /// <summary>
        /// Defines the <see cref="IsLoading"/> property.
        /// </summary>
        public static readonly DirectProperty<AdvancedImage, bool> IsLoadingProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImage, bool>(
                                                                 nameof(IsLoading),
                                                                 image => image._isLoading);

        /// <summary>
        /// Defines the <see cref="CurrentImage"/> property.
        /// </summary>
        public static readonly DirectProperty<AdvancedImage, IImage?> CurrentImageProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImage, IImage?>(
                                                                    nameof(CurrentImage),
                                                                    image => image._currentImage);

        /// <summary>
        /// Defines the <see cref="Stretch"/> property.
        /// </summary>
        public static readonly StyledProperty<Stretch> StretchProperty =
            Image.StretchProperty.AddOwner<AdvancedImage>();

        /// <summary>
        /// Defines the <see cref="StretchDirection"/> property.
        /// </summary>
        public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
            Image.StretchDirectionProperty.AddOwner<AdvancedImage>();

        static AdvancedImage()
        {
            AffectsRender<AdvancedImage>(CurrentImageProperty, StretchProperty, StretchDirectionProperty);
            AffectsMeasure<AdvancedImage>(CurrentImageProperty, StretchProperty, StretchDirectionProperty);

            var sourceChangedObservable = SourceProperty.Changed.Where(args => args.NewValue != args.OldValue);

            var loaderChangedObservable = LoaderProperty.Changed
                                                        .Where(args => args.OldValue.Value != args.NewValue.Value)
                                                        .Where((args, i) => i == 0 || args.Sender.GetValue(ShouldLoaderChangeTriggerUpdateProperty))
                                                        .Select(args => args.NewValue.Value)
                                                        .StartWith((IAsyncImageLoader?) null);

            sourceChangedObservable.CombineLatest(loaderChangedObservable)
                                   .Select(tuple => (Control: (AdvancedImage) tuple.First.Sender, Source: tuple.First.NewValue.Value, Loader: tuple.Second))
                                   .Subscribe(tuple => tuple.Control.UpdateImage(tuple.Source, tuple.Loader));
        }

        private CancellationTokenSource? _updateCancellationToken;

        private async void UpdateImage(string? source, IAsyncImageLoader? loader)
        {
            _updateCancellationToken?.Cancel();
            _updateCancellationToken?.Dispose();
            var cancellationTokenSource = _updateCancellationToken = new CancellationTokenSource();
            IsLoading    = true;
            CurrentImage = null;

            Bitmap? bitmap = null;
            if (source != null)
            {
                // Hack to support relative URI  TODO: Refactor IAsyncImageLoader to support BaseUri
                //var assetLoader = AvaloniaLocator.Current.GetService<AssetLoader>();
                
                try
                {
                    var uri = new Uri(source, UriKind.RelativeOrAbsolute);
                    if (AssetLoader.Exists(uri, _baseUri))
                    {
                        bitmap = new Bitmap(AssetLoader.Open(uri, _baseUri));
                    }
                    //else
                      //  throw new NullReferenceException($"{nameof(assetLoader)}");

                    // if (assetLoader?.Exists(uri, _baseUri) ?? throw new NullReferenceException($"{nameof(assetLoader)}"))
                    // {
                    //     bitmap = new Bitmap(assetLoader.Open(uri, _baseUri));
                    // }

                }
                catch (Exception)
                {
                    // ignored
                }

                loader ??= ImageLoader.AsyncImageLoader;
                bitmap ??= await loader.ProvideImageAsync(source);
            }

            if (cancellationTokenSource.IsCancellationRequested) return;
            CurrentImage = bitmap;
            IsLoading    = false;
        }

        private Uri? _baseUri;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvancedImage"/> class.
        /// </summary>
        /// <param name="baseUri">The base URL for the XAML context.</param>
        public AdvancedImage(Uri? baseUri) { _baseUri = baseUri; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvancedImage"/> class.
        /// </summary>
        /// <param name="serviceProvider">The XAML service provider.</param>
        public AdvancedImage(IServiceProvider serviceProvider)
            : this((serviceProvider.GetService(typeof(IUriContext)) as IUriContext)?.BaseUri)
        {
        }

        /// <summary>
        /// Gets or sets the URI for image that will be displayed.
        /// </summary>
        public IAsyncImageLoader? Loader
        {
            get => GetValue(LoaderProperty);
            set => SetValue(LoaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the URI for image that will be displayed.
        /// </summary>
        public string? Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private bool _shouldLoaderChangeTriggerUpdate;

        /// <summary>
        /// Gets or sets the value controlling whether the image should be reloaded after changing the loader.
        /// </summary>
        public bool ShouldLoaderChangeTriggerUpdate
        {
            get => _shouldLoaderChangeTriggerUpdate;
            set => SetAndRaise(ShouldLoaderChangeTriggerUpdateProperty, ref _shouldLoaderChangeTriggerUpdate, value);
        }

        private bool _isLoading;

        /// <summary>
        /// Gets a value indicating is image currently is loading state.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetAndRaise(IsLoadingProperty, ref _isLoading, value);
        }

        private IImage? _currentImage;

        /// <summary>
        /// Gets a currently loaded IImage.
        /// </summary>
        public IImage? CurrentImage
        {
            get => _currentImage;
            private set => SetAndRaise(CurrentImageProperty, ref _currentImage, value);
        }

        /// <summary>
        /// Gets or sets a value controlling how the image will be stretched.
        /// </summary>
        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        /// <summary>
        /// Gets or sets a value controlling in what direction the image will be stretched.
        /// </summary>
        public StretchDirection StretchDirection
        {
            get => GetValue(StretchDirectionProperty);
            set => SetValue(StretchDirectionProperty, value);
        }

        /// <summary>
        /// Renders the control.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(DrawingContext context)
        {
            var source = CurrentImage;
            if (source != null && Bounds.Width > 0 && Bounds.Height > 0)
            {
                Rect viewPort   = new Rect(Bounds.Size);
                Size sourceSize = source.Size;

                Vector scale      = Stretch.CalculateScaling(Bounds.Size, sourceSize, StretchDirection);
                Size   scaledSize = sourceSize * scale;
                Rect destRect = viewPort
                               .CenterRect(new Rect(scaledSize))
                               .Intersect(viewPort);
                Rect sourceRect = new Rect(sourceSize)
                   .CenterRect(new Rect(destRect.Size / scale));

                //var interpolationMode = RenderOptions.GetBitmapInterpolationMode(this);

                //context.DrawImage(source, sourceRect, destRect, interpolationMode);
                context.DrawImage(source, sourceRect, destRect);
            }
            else
            {
                base.Render(context);
            }
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize) => CurrentImage != null ?
                                                                           Stretch.CalculateSize(availableSize, CurrentImage.Size, StretchDirection) :
                                                                           base.MeasureOverride(availableSize);

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize) => CurrentImage != null ? Stretch.CalculateSize(finalSize, CurrentImage.Size) : base.ArrangeOverride(finalSize);

    }
}