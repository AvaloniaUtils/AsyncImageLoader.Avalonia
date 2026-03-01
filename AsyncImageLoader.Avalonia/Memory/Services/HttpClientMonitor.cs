using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncImageLoader.Memory.Services;

public sealed class HttpClientMonitor : IDisposable
    {
        public static HttpClientMonitor Instance { get; internal set; } = new();
        
        private readonly LinkedList<HttpRequestLog> _logs = new();
        private readonly TimeSpan _interval;
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        
        private readonly int _maxLogs = 100;

        public event Action<HttpRequestSnapshot>? SnapshotUpdated;

        public bool IsRunning => _cts != null;

        public HttpClientMonitor(TimeSpan? interval = null)
        {
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
            catch (OperationCanceledException) { }

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
            HttpRequestSnapshot snapshot;
            lock (_logs)
            {
                snapshot = new HttpRequestSnapshot(_logs.ToList());
            }

            SnapshotUpdated?.Invoke(snapshot);
        }

        public void Log(HttpRequestLog log)
        {
            lock (_logs)
            {
                
                _logs.AddLast(log);
                if (_logs.Count > _maxLogs)
                    _logs.RemoveFirst();
            }
        }

        public void Dispose()
        {
            _ = StopAsync();
        }
    }

    public sealed class HttpRequestSnapshot
    {
        public IReadOnlyList<HttpRequestLog> Requests { get; }

        public HttpRequestSnapshot(IReadOnlyList<HttpRequestLog> requests)
        {
            Requests = requests;
        }
    }

    public sealed class HttpRequestLog
    {
        public string Url { get; set; } = "";
        public HttpMethod Method { get; set; } = HttpMethod.Get;
        public int? StatusCode { get; set; }
        public TimeSpan Duration { get; set; }
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public override string ToString()
        {
            if (Exception != null)
                return $"[{Timestamp:HH:mm:ss}] {Method} {Url} FAILED: {Exception.Message}";
            return $"[{Timestamp:HH:mm:ss}] {Method} {Url} => {StatusCode} ({Duration.TotalMilliseconds:0}ms)";
        }
    }