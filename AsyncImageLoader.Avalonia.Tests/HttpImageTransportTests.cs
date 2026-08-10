using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using AwesomeAssertions;
using Xunit;

namespace AsyncImageLoader.Avalonia.Tests;

public sealed class HttpImageTransportTests {
    [Fact]
    public async Task DownloadsHttpResponseIntoOwnedStream() {
        using var client = new HttpClient(new TestHttpMessageHandler(_ =>
            TestHttpMessageHandler.CreateResponse(new byte[] { 1, 2, 3 })));
        var transport = new HttpImageTransport(client);

        using var stream = await transport.GetAsync(new ImageLoadRequest("https://example.test/image.png"));
        using var memory = new MemoryStream();
        await stream!.CopyToAsync(memory);

        memory.ToArray().Should().Equal(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public async Task DisposingResponseStreamDisposesResponse() {
        var response = new TrackingHttpResponseMessage(new ByteArrayContent(new byte[] { 1, 2, 3 }));
        using var client = new HttpClient(new TestHttpMessageHandler(_ => response));
        var transport = new HttpImageTransport(client);

        using (var stream = await transport.GetAsync(new ImageLoadRequest("https://example.test/image.png"))) {
            stream!.ReadByte();
            response.IsDisposed.Should().BeFalse();
        }

        response.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task IgnoresNonHttpSources() {
        using var client = new HttpClient(new TestHttpMessageHandler(_ =>
            TestHttpMessageHandler.CreateResponse(new byte[] { 1 })));
        var transport = new HttpImageTransport(client);

        var stream = await transport.GetAsync(new ImageLoadRequest("avares://App/image.png"));

        stream.Should().BeNull();
    }

    [Fact]
    public async Task ThrowsForUnsuccessfulResponse() {
        using var client = new HttpClient(new TestHttpMessageHandler(_ =>
            TestHttpMessageHandler.CreateResponse(Array.Empty<byte>(), HttpStatusCode.NotFound)));
        var transport = new HttpImageTransport(client);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            transport.GetAsync(new ImageLoadRequest("https://example.test/image.png")));
    }

    [Fact]
    public async Task PropagatesCancellation() {
        using var client = new HttpClient(new BlockingHttpMessageHandler());
        var transport = new HttpImageTransport(client);
        using var cancellation = new CancellationTokenSource();
        var task = transport.GetAsync(
            new ImageLoadRequest("https://example.test/image.png"),
            cancellation.Token);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    private sealed class BlockingHttpMessageHandler : HttpMessageHandler {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class TrackingHttpResponseMessage : HttpResponseMessage {
        public TrackingHttpResponseMessage(HttpContent content) {
            Content = content;
        }

        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing) {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
