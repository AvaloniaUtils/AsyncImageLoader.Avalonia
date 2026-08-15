using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless;

namespace AsyncImageLoader.Avalonia.Tests;

internal static class TestEnvironment {
    [ModuleInitializer]
    internal static void InitializeAvalonia() {
        AppBuilder.Configure<TestApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .SetupWithoutStarting();
    }

    private sealed class TestApplication : Application {
    }
}
