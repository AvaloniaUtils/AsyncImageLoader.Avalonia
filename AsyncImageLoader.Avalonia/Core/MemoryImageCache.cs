using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;

namespace AsyncImageLoader.Core;

/// <summary>
/// Caches decoded images and manages their consumer leases.
/// </summary>
public sealed class MemoryImageCache : IImageMemoryCache {
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly MemoryImageCacheOptions _options;
    private readonly Timer? _cleanupTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a memory image cache.
    /// </summary>
    public MemoryImageCache(MemoryImageCacheOptions? options = null) {
        _options = options ?? new MemoryImageCacheOptions();
        _options.Validate();

        if (_options.AbsoluteExpiration is not null || _options.SlidingExpiration is not null) {
            var period = GetCleanupPeriod();
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, period, period);
        }
    }

    /// <inheritdoc />
    public async Task<IImageLease?> GetOrCreateAsync(
        string key,
        Func<CancellationToken, Task<IImage?>> factory,
        CancellationToken cancellationToken = default) {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be empty.", nameof(key));
        ArgumentNullException.ThrowIfNull(factory);

        Entry entry;
        Task<IImage?> loadingTask;
        lock (_gate) {
            ThrowIfDisposed();

            if (!_entries.TryGetValue(key, out entry!)) {
                entry = new Entry();
                _entries.Add(key, entry);
            }

            if (entry.Image is not null) {
                if (entry.LeaseCount == 0 && IsExpired(entry))
                    DetachEntryLocked(key, entry);
                else {
                    Touch(entry);
                    entry.LeaseCount++;
                    return new MemoryImageLease(entry.Image, () => Release(key, entry));
                }
            }

            if (!_entries.TryGetValue(key, out entry!)) {
                entry = new Entry();
                _entries.Add(key, entry);
            }

            entry.LoadingTask ??= factory(cancellationToken);
            loadingTask = entry.LoadingTask;
        }

        IImage? image;
        try {
            image = await loadingTask.ConfigureAwait(false);
        }
        catch {
            lock (_gate) {
                if (_entries.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
                    _entries.Remove(key);
            }

            throw;
        }

        lock (_gate) {
            if (_disposed) {
                DisposeImage(image);
                return null;
            }

            if (image is null) {
                RemoveEntryLocked(key, entry);
                return null;
            }

            if (entry.Image is null)
                entry.Image = image;
            else if (!ReferenceEquals(entry.Image, image))
                DisposeImage(image);

            entry.LoadingTask = null;
            entry.StrongSince = DateTimeOffset.UtcNow;
            entry.LeaseCount++;
            entry.LastAccess = entry.StrongSince;
            return new MemoryImageLease(entry.Image, () => Release(key, entry));
        }
    }

    /// <inheritdoc />
    public void Clear() {
        lock (_gate) {
            foreach (var pair in new List<KeyValuePair<string, Entry>>(_entries)) {
                DetachEntryLocked(pair.Key, pair.Value);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        lock (_gate) {
            if (_disposed)
                return;

            _disposed = true;
            _cleanupTimer?.Dispose();
            foreach (var pair in new List<KeyValuePair<string, Entry>>(_entries)) {
                DetachEntryLocked(pair.Key, pair.Value);
            }
        }
    }

    private void Release(string key, Entry entry) {
        lock (_gate) {
            if (entry.LeaseCount > 0)
                entry.LeaseCount--;

            if (entry.LeaseCount == 0 && (_disposed || entry.IsDetached || IsExpired(entry)))
                DisposeDetachedEntry(entry);
        }
    }

    private void CleanupExpiredEntries(object? state) {
        lock (_gate) {
            if (_disposed)
                return;

            foreach (var pair in new List<KeyValuePair<string, Entry>>(_entries)) {
                if (pair.Value.LeaseCount == 0 && pair.Value.LoadingTask is null && IsExpired(pair.Value))
                    RemoveEntryLocked(pair.Key, pair.Value);
            }
        }
    }

    private void Touch(Entry entry) {
        var now = DateTimeOffset.UtcNow;
        if (_options.SlidingExpiration is not null && !IsAbsoluteExpired(entry, now))
            entry.LastAccess = now;
    }

    private bool IsExpired(Entry entry) {
        var now = DateTimeOffset.UtcNow;
        return IsAbsoluteExpired(entry, now) ||
               _options.SlidingExpiration is { } sliding && now - entry.LastAccess >= sliding;
    }

    private bool IsAbsoluteExpired(Entry entry, DateTimeOffset now) {
        return _options.AbsoluteExpiration is { } absolute && now - entry.StrongSince >= absolute;
    }

    private TimeSpan GetCleanupPeriod() {
        var period = _options.AbsoluteExpiration ?? _options.SlidingExpiration ?? TimeSpan.FromMinutes(1);
        period = TimeSpan.FromTicks(Math.Max(period.Ticks / 2, TimeSpan.FromMilliseconds(100).Ticks));
        return period > TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : period;
    }

    private void RemoveEntryLocked(string key, Entry entry) {
        if (!_entries.TryGetValue(key, out var current) || !ReferenceEquals(current, entry))
            return;

        _entries.Remove(key);
        entry.IsDetached = true;
        if (entry.LeaseCount == 0 && entry.LoadingTask is null)
            DisposeDetachedEntry(entry);
    }

    private void DetachEntryLocked(string key, Entry entry) {
        if (_entries.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
            _entries.Remove(key);

        entry.IsDetached = true;
        if (entry.LeaseCount == 0 && entry.LoadingTask is null)
            DisposeDetachedEntry(entry);
    }

    private static void DisposeDetachedEntry(Entry entry) {
        DisposeImage(entry.Image);
        entry.Image = null;
    }

    private static void DisposeImage(IImage? image) {
        (image as IDisposable)?.Dispose();
    }

    private void ThrowIfDisposed() {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MemoryImageCache));
    }

    private sealed class Entry {
        public IImage? Image;
        public Task<IImage?>? LoadingTask;
        public int LeaseCount;
        public DateTimeOffset StrongSince;
        public DateTimeOffset LastAccess;
        public bool IsDetached;
    }
}
