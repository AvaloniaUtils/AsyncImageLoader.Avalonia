using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core.Pipeline;

namespace AsyncImageLoader.Core.Transport;

/// <summary>
/// Downloads image data over HTTP or HTTPS.
/// </summary>
public sealed class HttpImageTransport : IImageTransport {
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes an HTTP image transport.
    /// </summary>
    public HttpImageTransport(HttpClient httpClient) {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Stream?> GetAsync(
        ImageLoadRequest request,
        CancellationToken cancellationToken = default) {
        if (!Uri.TryCreate(request.Source, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;

        var response = await _httpClient.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        try {
            response.EnsureSuccessStatusCode();
            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            return new HttpResponseStream(responseStream, response);
        }
        catch {
            response.Dispose();
            throw;
        }
    }
}
