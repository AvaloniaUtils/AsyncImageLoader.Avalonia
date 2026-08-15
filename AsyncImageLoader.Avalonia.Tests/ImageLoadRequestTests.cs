using System;
using AsyncImageLoader.Core;
using AsyncImageLoader.Core.Pipeline;
using AwesomeAssertions;
using Xunit;

namespace AsyncImageLoader.Avalonia.Tests;

public sealed class ImageLoadRequestTests {
    [Fact]
    public void KeepsSourceResolutionContext() {
        var baseUri = new Uri("avares://Test/Assets/");

        var request = new ImageLoadRequest("icon.png", baseUri);

        request.Source.Should().Be("icon.png");
        request.BaseUri.Should().Be(baseUri);
        request.StorageProvider.Should().BeNull();
    }

    [Fact]
    public void RejectsEmptySource() {
        var action = () => new ImageLoadRequest(" ");

        action.Should().Throw<ArgumentException>();
    }
}
