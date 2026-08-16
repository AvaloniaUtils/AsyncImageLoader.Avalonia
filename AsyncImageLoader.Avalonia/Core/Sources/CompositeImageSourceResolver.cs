using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core.Pipeline;

namespace AsyncImageLoader.Core.Sources;

/// <summary>
/// Tries source resolvers in the order supplied by the caller.
/// </summary>
public sealed class CompositeImageSourceResolver : IImageSourceResolver {
    private readonly IReadOnlyList<IImageSourceResolver> _resolvers;

    /// <summary>
    /// Initializes a composite resolver.
    /// </summary>
    public CompositeImageSourceResolver(params IImageSourceResolver[] resolvers) {
        _resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
    }

    /// <inheritdoc />
    public async Task<ResolvedImageSource?> ResolveAsync(
        ImageLoadRequest request,
        CancellationToken cancellationToken = default) {
        foreach (var resolver in _resolvers) {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await resolver.ResolveAsync(request, cancellationToken).ConfigureAwait(false);
            if (result is not null)
                return result;
        }

        return null;
    }
}
