using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncImageLoader.Core;

/// <summary>
/// Stores encoded image data in a directory on the local filesystem.
/// </summary>
public sealed class DiskImageByteCache : IImageByteCache {
    private readonly string _directory;

    /// <summary>
    /// Initializes a disk cache.
    /// </summary>
    public DiskImageByteCache(string directory) {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Cache directory cannot be empty.", nameof(directory));

        _directory = Path.GetFullPath(directory);
    }

    /// <inheritdoc />
    public async Task<Stream?> GetAsync(
        string key,
        CancellationToken cancellationToken = default) {
        var path = GetPath(key);
        if (!File.Exists(path))
            return null;

        try {
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            return new MemoryStream(bytes, writable: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (IOException) {
            return null;
        }
        catch (UnauthorizedAccessException) {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync(
        string key,
        Stream data,
        CancellationToken cancellationToken = default) {
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        Directory.CreateDirectory(_directory);
        var path = GetPath(key);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try {
            await using (var file = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             81920,
                             FileOptions.Asynchronous | FileOptions.SequentialScan)) {
                await data.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, path, true);
        }
        finally {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetPath(key);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Clear() {
        if (!Directory.Exists(_directory))
            return;

        foreach (var file in Directory.EnumerateFiles(_directory))
            File.Delete(file);
    }

    private string GetPath(string key) {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be empty.", nameof(key));
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Path.Combine(_directory, Convert.ToHexString(hash));
    }
}
