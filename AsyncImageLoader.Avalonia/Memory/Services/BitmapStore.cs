using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader.Memory.Services;

using System.Collections.Concurrent;

public sealed class BitmapStore
{
    private readonly ConcurrentDictionary<string, BitmapEntry> _bitmaps = new();

    internal ConcurrentBag<BitmapEntry> BitmapEntries { get; } = new();

    public static BitmapStore Instance { get; } = new BitmapStore();
    private BitmapStore() { }

    public bool TryGet(string key, out BitmapEntry entry)
    {
        if (_bitmaps.TryGetValue(key, out var node))
        {
            entry = node;
            return true;
        }

        entry = null!;
        return false;
    }

    public void TryAdd(BitmapEntry entry, bool acquire = false)
    {
        if(acquire)
            entry.Acquire();
        
        _bitmaps.TryAdd(entry.Key, entry);
    }

    public IEnumerable<BitmapEntry> EnumerateFromOldest()
    {
        BitmapEntry[] snapshot;
        snapshot = _bitmaps.Values.Concat(BitmapEntries).ToArray();
        
        return snapshot.OrderBy(x => x.RefCount).ToArray();
    }

    public void Remove(string key) {
        _bitmaps.TryRemove(key, out var node);
    }

    public void AddBitmapEntry(BitmapEntry bitmapEntry)
    {
        BitmapEntries.Add(bitmapEntry);
    }

    public void RemoveBitmapEntry(string url)
    {
        var toRemove = BitmapEntries.Where(x => x.Key == url).ToList();
        
        foreach (var entry in toRemove)
            BitmapEntries.TryTake(out _);
    }
}

public sealed class BitmapEntry : IDisposable
{
    public string Key { get; }
    public Bitmap Bitmap { get; }

    private int _refCount;
    public int RefCount => _refCount;
    
    public DateTime LastReleased { get; private set; }

    public BitmapEntry(string key, Bitmap bitmap)
    {
        Key = key;
        Bitmap = bitmap;
    }

    public void Acquire() {
        Interlocked.Increment(ref _refCount);
    }

    public void Release() {
        if (Interlocked.Decrement(ref _refCount) == 0)
            LastReleased = DateTime.UtcNow;
    }

    public void Dispose() {
        Bitmap.Dispose();
    }
}

public sealed class BitmapLease : IDisposable 
{
    private readonly BitmapEntry _entry;
    private int _disposed;

    public Bitmap? Bitmap
    {
        get
        {
            if (Volatile.Read(ref _disposed) == 1)
                return null;

            return _entry.Bitmap;
        }
    }


    public BitmapLease(BitmapEntry entry) {
        _entry = entry;
        _entry.Acquire();
    }

    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;
        
        _entry.Release();
    }
}