using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using AsyncImageLoader.Core.Caching;
using AwesomeAssertions;
using Xunit;

namespace AsyncImageLoader.Avalonia.Tests;

public sealed class DiskImageByteCacheTests {
    [Fact]
    public async Task StoresAndReadsEncodedData() {
        var directory = CreateDirectory();
        try {
            var cache = new DiskImageByteCache(directory);
            await using var input = new MemoryStream(Encoding.UTF8.GetBytes("image"));

            await cache.SetAsync("https://example.test/ä.png", input);
            using var output = await cache.GetAsync("https://example.test/ä.png");
            using var reader = new StreamReader(output!);

            (await reader.ReadToEndAsync()).Should().Be("image");
        }
        finally {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task MissingDataIsCacheMiss() {
        var directory = CreateDirectory();
        try {
            var cache = new DiskImageByteCache(directory);

            var result = await cache.GetAsync("missing");

            result.Should().BeNull();
        }
        finally {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task RemovesData() {
        var directory = CreateDirectory();
        try {
            var cache = new DiskImageByteCache(directory);
            await using var input = new MemoryStream(new byte[] { 1 });
            await cache.SetAsync("image", input);

            await cache.RemoveAsync("image");

            (await cache.GetAsync("image")).Should().BeNull();
        }
        finally {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task FailedWriteDoesNotLeaveFinalEntry() {
        var directory = CreateDirectory();
        try {
            var cache = new DiskImageByteCache(directory);
            await using var input = new ThrowingStream();

            await Assert.ThrowsAsync<IOException>(() => cache.SetAsync("image", input));

            (await cache.GetAsync("image")).Should().BeNull();
        }
        finally {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateDirectory() {
        return Directory.CreateTempSubdirectory("async-image-cache-").FullName;
    }

    private sealed class ThrowingStream : MemoryStream {
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) {
            throw new IOException("Test failure");
        }
    }
}
