using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using AsyncImageLoader.Core.Pipeline;
using AsyncImageLoader.Loaders;
using AwesomeAssertions;
using Xunit;

namespace AsyncImageLoader.Avalonia.Tests;

#pragma warning disable CS0618 // Compatibility facade remains covered until removal.
public sealed class DiskCachedWebImageLoaderTests {
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task ReloadsFromDiskAfterRamCacheIsCleared() {
        var directory = Directory.CreateTempSubdirectory("disk-loader-").FullName;
        try {
            var requests = 0;
            using var client = new HttpClient(new TestHttpMessageHandler(_ => {
                Interlocked.Increment(ref requests);
                return TestHttpMessageHandler.CreateResponse(Png);
            }));
            using var loader = new DiskCachedWebImageLoader(client, false, directory);

            using var first = await loader.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));
            loader.ClearRamCache();
            using var second = await loader.LoadAsync(new ImageLoadRequest("https://example.test/image.png"));

            first.Should().NotBeNull();
            second.Should().NotBeNull();
            second.Image.Should().NotBeSameAs(first.Image);
            requests.Should().Be(1);
        }
        finally {
            Directory.Delete(directory, true);
        }
    }
}
#pragma warning restore CS0618
