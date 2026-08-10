using System;
using System.IO;
using System.Threading.Tasks;
using AsyncImageLoader.Core;
using AwesomeAssertions;
using Xunit;

namespace AsyncImageLoader.Avalonia.Tests;

public sealed class SourceResolverTests {
    [Fact]
    public async Task FileResolverReadsExistingFile() {
        var path = Path.GetTempFileName();
        try {
            await File.WriteAllBytesAsync(path, new byte[] { 1, 2, 3 });
            var resolver = new FileImageSourceResolver();

            using var result = await resolver.ResolveAsync(new ImageLoadRequest(path));

            result.Should().NotBeNull();
            using var memory = new MemoryStream();
            await result!.Stream.CopyToAsync(memory);
            memory.ToArray().Should().Equal(new byte[] { 1, 2, 3 });
        }
        finally {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task FileResolverReturnsNullForMissingFile() {
        var resolver = new FileImageSourceResolver();

        var result = await resolver.ResolveAsync(new ImageLoadRequest(Path.Combine(
            Path.GetTempPath(), Guid.NewGuid().ToString("N"))));

        result.Should().BeNull();
    }

    [Fact]
    public async Task FileResolverReadsFileUriWithoutStorageProvider() {
        var path = Path.GetTempFileName();
        try {
            await File.WriteAllBytesAsync(path, new byte[] { 4, 5, 6 });
            var resolver = new FileImageSourceResolver();

            using var result = await resolver.ResolveAsync(new ImageLoadRequest(new Uri(path).AbsoluteUri));

            result.Should().NotBeNull();
            using var memory = new MemoryStream();
            await result!.Stream.CopyToAsync(memory);
            memory.ToArray().Should().Equal(new byte[] { 4, 5, 6 });
        }
        finally {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task AssetResolverDoesNotHandleHttpUri() {
        var resolver = new AvaloniaAssetSourceResolver();

        var result = await resolver.ResolveAsync(new ImageLoadRequest("https://example.test/image.png"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task CompositeResolverUsesTheFirstMatchingResolver() {
        var expected = new ResolvedImageSource(new MemoryStream(new byte[] { 7 }));
        var first = new StubResolver(expected);
        var second = new StubResolver(new ResolvedImageSource(new MemoryStream(new byte[] { 8 })));
        var resolver = new CompositeImageSourceResolver(first, second);

        var result = await resolver.ResolveAsync(new ImageLoadRequest("image"));

        result.Should().BeSameAs(expected);
        second.Calls.Should().Be(0);
        result!.Dispose();
    }

    private sealed class StubResolver : IImageSourceResolver {
        private readonly ResolvedImageSource _result;

        public StubResolver(ResolvedImageSource result) {
            _result = result;
        }

        public int Calls { get; private set; }

        public Task<ResolvedImageSource?> ResolveAsync(
            ImageLoadRequest request,
            System.Threading.CancellationToken cancellationToken = default) {
            Calls++;
            return Task.FromResult<ResolvedImageSource?>(_result);
        }
    }
}
