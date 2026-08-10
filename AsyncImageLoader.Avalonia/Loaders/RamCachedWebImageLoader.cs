using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace AsyncImageLoader.Loaders;

/// <summary>
///     Provides memory cached way to asynchronously load images for <see cref="ImageLoader" />
///     Can be used as base class if you want to create custom in memory caching
/// </summary>
public class RamCachedWebImageLoader : BaseWebImageLoader {
    private readonly ConcurrentDictionary<string, CacheEntry> _memoryCache = new();
    private readonly RamCacheOptions _options;
    private readonly Timer? _cleanupTimer;

    /// <inheritdoc />
    public RamCachedWebImageLoader() : this(null) { }

    /// <summary>
    ///     Initializes a loader with the specified memory retention policy.
    /// </summary>
    public RamCachedWebImageLoader(RamCacheOptions? options) {
        _options = options ?? new RamCacheOptions();
        _options.Validate();

        if (_options.AbsoluteExpiration is not null || _options.SlidingExpiration is not null)
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, GetCleanupPeriod(), GetCleanupPeriod());
    }

    /// <inheritdoc />
    public RamCachedWebImageLoader(HttpClient httpClient, bool disposeHttpClient) : base(httpClient,
        disposeHttpClient) {
        _options = new RamCacheOptions();
    }

    /// <summary>
    ///     Initializes a loader with a custom HTTP client and memory retention policy.
    /// </summary>
    public RamCachedWebImageLoader(HttpClient httpClient, bool disposeHttpClient, RamCacheOptions? options)
        : base(httpClient, disposeHttpClient) {
        _options = options ?? new RamCacheOptions();
        _options.Validate();

        if (_options.AbsoluteExpiration is not null || _options.SlidingExpiration is not null)
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, GetCleanupPeriod(), GetCleanupPeriod());
    }

    /// <inheritdoc />
    public override async Task<Bitmap?> ProvideImageAsync(string url) {
        return await GetOrLoadAsync(url, () => LoadAsync(url)).ConfigureAwait(false);
    }

    public override async Task<Bitmap?> ProvideImageAsync(string url, IStorageProvider? storageProvider = null) {
        return await GetOrLoadAsync(url, () => LoadAsync(url, storageProvider)).ConfigureAwait(false);
    }

    public void ClearRamCache() {
        _memoryCache.Clear();
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _cleanupTimer?.Dispose();
            _memoryCache.Clear();
        }

        base.Dispose(disposing);
    }

    private async Task<Bitmap?> GetOrLoadAsync(string url, Func<Task<Bitmap?>> load) {
        while (true) {
            var now = DateTimeOffset.UtcNow;
            if (_memoryCache.TryGetValue(url, out var existing)) {
                Lazy<Task<Bitmap?>> loading;
                lock (existing) {
                    if (existing.StrongBitmap is not null && !IsExpired(existing, now)) {
                        Touch(existing, now);
                        return existing.StrongBitmap;
                    }

                    if (existing.WeakBitmap is { } weakReference && weakReference.TryGetTarget(out var weakBitmap)) {
                        if (_options.SlidingExpiration is not null && !IsAbsoluteExpired(existing, now)) {
                            existing.StrongBitmap = weakBitmap;
                            Touch(existing, now);
                        }

                        return weakBitmap;
                    }

                    existing.LoadingTask ??= new Lazy<Task<Bitmap?>>(load,
                        LazyThreadSafetyMode.ExecutionAndPublication);
                    loading = existing.LoadingTask;
                }

                return await LoadEntryAsync(url, existing, loading).ConfigureAwait(false);
            }

            var entry = new CacheEntry();
            if (_memoryCache.TryAdd(url, entry)) {
                entry.LoadingTask = new Lazy<Task<Bitmap?>>(load,
                    LazyThreadSafetyMode.ExecutionAndPublication);
                return await LoadEntryAsync(url, entry, entry.LoadingTask).ConfigureAwait(false);
            }
        }
    }

    private async Task<Bitmap?> LoadEntryAsync(
        string url,
        CacheEntry entry,
        Lazy<Task<Bitmap?>> loading) {
        try {
            var bitmap = await loading.Value.ConfigureAwait(false);
            CompleteLoad(url, entry, bitmap);
            return bitmap;
        }
        catch {
            _memoryCache.TryRemove(new KeyValuePair<string, CacheEntry>(url, entry));
            throw;
        }
    }

    private void CompleteLoad(string url, CacheEntry entry, Bitmap? bitmap) {
        lock (entry) {
            entry.LoadingTask = null;
            if (bitmap is not null) {
                entry.StrongBitmap = bitmap;
                entry.StrongSince = DateTimeOffset.UtcNow;
                entry.LastAccess = entry.StrongSince;
                entry.WeakBitmap = null;
            }
        }

        if (bitmap is null)
            _memoryCache.TryRemove(new KeyValuePair<string, CacheEntry>(url, entry));
    }

    private bool IsExpired(CacheEntry entry, DateTimeOffset now) {
        return IsAbsoluteExpired(entry, now) ||
               _options.SlidingExpiration is { } sliding && now - entry.LastAccess >= sliding;
    }

    private bool IsAbsoluteExpired(CacheEntry entry, DateTimeOffset now) {
        return _options.AbsoluteExpiration is { } absolute && now - entry.StrongSince >= absolute;
    }

    private void Touch(CacheEntry entry, DateTimeOffset now) {
        if (_options.SlidingExpiration is not null)
            entry.LastAccess = now;
    }

    private void CleanupExpiredEntries(object? state) {
        var now = DateTimeOffset.UtcNow;
        foreach (var pair in _memoryCache) {
            var entry = pair.Value;
            lock (entry) {
                if (entry.LoadingTask is not null || entry.StrongBitmap is null || !IsExpired(entry, now))
                    continue;

                entry.WeakBitmap = new WeakReference<Bitmap>(entry.StrongBitmap);
                entry.StrongBitmap = null;
            }
        }
    }

    private TimeSpan GetCleanupPeriod() {
        var period = _options.AbsoluteExpiration ?? _options.SlidingExpiration ?? TimeSpan.FromMinutes(1);
        period = TimeSpan.FromTicks(Math.Max(period.Ticks / 2, TimeSpan.FromMilliseconds(100).Ticks));
        return period > TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : period;
    }

    private sealed class CacheEntry {
        public Lazy<Task<Bitmap?>>? LoadingTask;
        public Bitmap? StrongBitmap;
        public WeakReference<Bitmap>? WeakBitmap;
        public DateTimeOffset StrongSince;
        public DateTimeOffset LastAccess;
    }
}
