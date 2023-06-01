﻿using System;
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
        ///     Defines the <see cref="Loader" /> property.
        /// </summary>
        public static readonly StyledProperty<IAsyncImageLoader?> LoaderProperty =
            AvaloniaProperty.Register<AdvancedImage, IAsyncImageLoader?>(nameof(Loader));

        /// <summary>
        ///     Defines the <see cref="Source" /> property.
        /// </summary>
        public static readonly StyledProperty<string?> SourceProperty =
            AvaloniaProperty.Register<AdvancedImage, string?>(nameof(Source));

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
        public static readonly DirectProperty<AdvancedImage, Bitmap?> CurrentImageProperty =
            AvaloniaProperty.RegisterDirect<AdvancedImage, Bitmap?>(
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

        private Bitmap? _currentImage;
        private bool _isCornerRadiusUsed;

        private bool _isLoading;

        private bool _shouldLoaderChangeTriggerUpdate;

        private CancellationTokenSource? _updateCancellationToken;

        static AdvancedImage()
        {
            AffectsRender<AdvancedImage>(CurrentImageProperty, StretchProperty, StretchDirectionProperty,
                CornerRadiusProperty);
            AffectsMeasure<AdvancedImage>(CurrentImageProperty, StretchProperty, StretchDirectionProperty);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AdvancedImage" /> class.
        /// </summary>
        /// <param name="baseUri">The base URL for the XAML context.</param>
        public AdvancedImage(Uri? baseUri)
        {
            _baseUri = baseUri;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AdvancedImage" /> class.
        /// </summary>
        /// <param name="serviceProvider">The XAML service provider.</param>
        public AdvancedImage(IServiceProvider serviceProvider)
            : this((serviceProvider.GetService(typeof(IUriContext)) as IUriContext)?.BaseUri)
        {
        }

        /// <summary>
        ///     Gets or sets the URI for image that will be displayed.
        /// </summary>
        public IAsyncImageLoader? Loader
        {
            get => GetValue(LoaderProperty);
            set => SetValue(LoaderProperty, value);
        }

        /// <summary>
        ///     Gets or sets the URI for image that will be displayed.
        /// </summary>
        public string? Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        ///     Gets or sets the value controlling whether the image should be reloaded after changing the loader.
        /// </summary>
        public bool ShouldLoaderChangeTriggerUpdate
        {
            get => _shouldLoaderChangeTriggerUpdate;
            set => SetAndRaise(ShouldLoaderChangeTriggerUpdateProperty, ref _shouldLoaderChangeTriggerUpdate, value);
        }

        /// <summary>
        ///     Gets a value indicating is image currently is loading state.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetAndRaise(IsLoadingProperty, ref _isLoading, value);
        }

        /// <summary>
        ///     Gets a currently loaded IImage.
        /// </summary>
        public Bitmap? CurrentImage
        {
            get => _currentImage;
            private set => SetAndRaise(CurrentImageProperty, ref _currentImage, value);
        }

        /// <summary>
        ///     Gets or sets a value controlling how the image will be stretched.
        /// </summary>
        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        /// <summary>
        ///     Gets or sets a value controlling in what direction the image will be stretched.
        /// </summary>
        public StretchDirection StretchDirection
        {
            get => GetValue(StretchDirectionProperty);
            set => SetValue(StretchDirectionProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == SourceProperty)
                UpdateImage(change.GetNewValue<string>(), Loader);
            else if (change.Property == LoaderProperty && ShouldLoaderChangeTriggerUpdate)
                UpdateImage(change.GetNewValue<string>(), Loader);
            else if (change.Property == CornerRadiusProperty)
                UpdateCornerRadius(change.GetNewValue<CornerRadius>());
            else if (change.Property == BoundsProperty && !(CornerRadius == default)) UpdateCornerRadius(CornerRadius);
            base.OnPropertyChanged(change);
        }

        private async void UpdateImage(string? source, IAsyncImageLoader? loader)
        {
            _updateCancellationToken?.Cancel();
            _updateCancellationToken?.Dispose();
            var cancellationTokenSource = _updateCancellationToken = new CancellationTokenSource();
            IsLoading = true;
            CurrentImage = null;

            Bitmap? bitmap = null;
            if (source != null)
            {
                // Hack to support relative URI
                // TODO: Refactor IAsyncImageLoader to support BaseUri 
                try
                {
                    var uri = new Uri(source, UriKind.RelativeOrAbsolute);
                    if (AssetLoader.Exists(uri, _baseUri)) bitmap = new Bitmap(AssetLoader.Open(uri, _baseUri));
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
            IsLoading = false;
        }

        private void UpdateCornerRadius(CornerRadius radius)
        {
            _isCornerRadiusUsed = !(radius == default);
            _cornerRadiusClip = new RoundedRect(new Rect(0, 0, Bounds.Width, Bounds.Height), radius);
        }

        /// <summary>
        ///     Renders the control.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(DrawingContext context)
        {
            var source = CurrentImage;

            if (source != null && Bounds is { Width: > 0, Height: > 0 })
            {
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
            else
            {
                base.Render(context);
            }
        }

        /// <summary>
        ///     Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            return CurrentImage != null
                ? Stretch.CalculateSize(availableSize, CurrentImage.Size, StretchDirection)
                : base.MeasureOverride(availableSize);
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            return CurrentImage != null
                ? Stretch.CalculateSize(finalSize, CurrentImage.Size)
                : base.ArrangeOverride(finalSize);
        }
    }
}