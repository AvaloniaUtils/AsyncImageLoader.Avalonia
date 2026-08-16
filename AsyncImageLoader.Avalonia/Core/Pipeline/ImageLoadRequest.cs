using System;
using Avalonia.Platform.Storage;

namespace AsyncImageLoader.Core.Pipeline;

/// <summary>
/// Describes an image loading request and the context required to resolve it.
/// </summary>
public sealed record ImageLoadRequest {
    /// <summary>
    /// Initializes a new image loading request.
    /// </summary>
    public ImageLoadRequest(
        string source,
        Uri? baseUri = null,
        IStorageProvider? storageProvider = null) {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Image source cannot be empty.", nameof(source));

        Source = source;
        BaseUri = baseUri;
        StorageProvider = storageProvider;
    }

    /// <summary>
    /// Gets the source to resolve.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the base URI used to resolve relative Avalonia resources.
    /// </summary>
    public Uri? BaseUri { get; }

    /// <summary>
    /// Gets the storage provider used to resolve platform storage URIs.
    /// </summary>
    public IStorageProvider? StorageProvider { get; }
}
