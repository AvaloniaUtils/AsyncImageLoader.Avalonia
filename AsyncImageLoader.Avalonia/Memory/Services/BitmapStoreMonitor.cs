using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader.Memory.Services;

public sealed class BitmapStoreMonitor : IDisposable
{
    private readonly BitmapStore _store;
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public event Action<BitmapStoreSnapshot>? SnapshotUpdated;

    public bool IsRunning => _cts != null;

    public BitmapStoreMonitor(
        BitmapStore? store = null,
        TimeSpan? interval = null)
    {
        _store = store ?? BitmapStore.Instance;
        _interval = interval ?? TimeSpan.FromSeconds(2);
    }

    public void Start()
    {
        if (_cts != null)
            return;

        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => LoopAsync(_cts.Token));
    }

    public async Task StopAsync()
    {
        if (_cts == null)
            return;

        _cts.Cancel();

        try
        {
            if (_loopTask != null)
                await _loopTask;
        }
        catch (OperationCanceledException)
        {
        }

        _cts.Dispose();
        _cts = null;
        _loopTask = null;
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            PublishSnapshot();

            try
            {
                await Task.Delay(_interval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void PublishSnapshot()
    {
        var entries = _store
            .EnumerateFromOldest()
            .Select(e => {
                var bytes = Estimate(e.Bitmap);
                
                return new BitmapEntryInfo(
                    e.Key,
                    e.RefCount,
                    e.LastReleased,
                    bytes);
            })
            .ToList();

        var total = entries.Sum(x => x.EstimatedBytes);

        SnapshotUpdated?.Invoke(
            new BitmapStoreSnapshot(entries, total));
    }
    
    public static long Estimate(Bitmap bmp)
    {
        var size = bmp.PixelSize;
        return (long)size.Width * size.Height * 4;
    }

    public void Dispose()
    {
        _ = StopAsync();
    }
}

public sealed class BitmapStoreSnapshot
{
    public IReadOnlyList<BitmapEntryInfo> Items { get; }
    public long TotalEstimatedBytes { get; }
    
    public string TotalEstimatedSizeFormatted =>
        $"{TotalEstimatedBytes / 1024.0 / 1024.0:0.00} MB";

    public BitmapStoreSnapshot(
        IReadOnlyList<BitmapEntryInfo> items,
        long totalEstimatedBytes)
    {
        Items = items;
        TotalEstimatedBytes = totalEstimatedBytes;
    }
}

public sealed class BitmapEntryInfo
{
    public string Key { get; }
    public int RefCount { get; }
    public DateTime LastReleased { get; }
    
    public long EstimatedBytes { get; }
    
    public string EstimatedSizeFormatted =>
        $"{EstimatedBytes / 1024.0 / 1024.0:0.00} MB";
    
    public string LastReleasedFormatted =>
        LastReleased == default
            ? "-"
            : LastReleased.ToLocalTime().ToString("HH:mm:ss");

    public BitmapEntryInfo(
        string key,
        int refCount,
        DateTime lastReleased,
        long estimatedBytes)
    {
        Key = key;
        RefCount = refCount;
        LastReleased = lastReleased;
        EstimatedBytes = estimatedBytes;
    }
}

