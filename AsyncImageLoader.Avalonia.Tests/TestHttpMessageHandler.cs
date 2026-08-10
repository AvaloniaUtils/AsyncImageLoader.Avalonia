using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncImageLoader.Avalonia.Tests;

internal sealed class TestHttpMessageHandler : HttpMessageHandler {
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) {
        _responseFactory = responseFactory;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_responseFactory(request));
    }

    public static HttpResponseMessage CreateResponse(byte[] content, HttpStatusCode statusCode = HttpStatusCode.OK) {
        return new HttpResponseMessage(statusCode) {
            Content = new ByteArrayContent(content)
        };
    }
}
