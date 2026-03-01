using System;
using System.Collections.ObjectModel;
using AsyncImageLoader.Memory.Services;

namespace AsyncImageLoader.ViewModels;

public class BitmapInspectorDesignViewModel : IDisposable
{
    private readonly BitmapStoreMonitor _monitor = new();

    public ObservableCollection<BitmapEntryInfo> Items { get; }
        = new();

    public ObservableCollection<HttpRequestLog> HttpLogs { get; } 
        = new();
    
    public BitmapInspectorDesignViewModel ()
    {
        _monitor.SnapshotUpdated += OnSnapshot;
        _monitor.Start();
        
        HttpClientMonitor.Instance.Start();
        HttpClientMonitor.Instance.SnapshotUpdated += OnHttpLog;
    }

    private void OnSnapshot(BitmapStoreSnapshot snapshot)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Items.Clear();

            foreach (var item in snapshot.Items)
                Items.Add(item);
        });
    }
    
    private void OnHttpLog(HttpRequestSnapshot snapshot)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            HttpLogs.Clear();

            foreach (var item in snapshot.Requests)
                HttpLogs.Add(item);
        });
    }

    ~BitmapInspectorDesignViewModel() {
        Dispose();
    }
    

    public void Dispose() {
        _monitor.SnapshotUpdated -= OnSnapshot;
        HttpClientMonitor.Instance.SnapshotUpdated -= OnHttpLog;
        
        _monitor.Dispose();
        HttpClientMonitor.Instance.Dispose();
    }
}

