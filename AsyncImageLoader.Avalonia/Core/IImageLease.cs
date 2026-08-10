using System;
using Avalonia.Media;

namespace AsyncImageLoader.Core;

/// <summary>
/// Owns one consumer reference to an image managed by a loader or cache.
/// </summary>
public interface IImageLease : IDisposable {
    /// <summary>
    /// Gets the image held by this lease.
    /// </summary>
    IImage Image { get; }
}
