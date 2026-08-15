using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using AwesomeAssertions;
using Avalonia.Media.Imaging;
using Xunit;

namespace AsyncImageLoader.Avalonia.Tests;

public sealed class BitmapDecoderTests {
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task DecodesBitmapFromStream() {
        var decoder = new BitmapDecoder();
        using var stream = new MemoryStream(Png);

        using var bitmap = await decoder.DecodeAsync(stream);

        bitmap.Size.Width.Should().Be(1);
        bitmap.Size.Height.Should().Be(1);
    }

    [Fact]
    public async Task DoesNotDecodeAfterCancellation() {
        var decoder = new BitmapDecoder();
        using var stream = new MemoryStream(Png);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => decoder.DecodeAsync(stream, cancellation.Token));
    }
}
