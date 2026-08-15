using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncImageLoader.Avalonia.Tests;

internal sealed class TestTimeProvider : TimeProvider {
    private readonly List<TestTimer> _timers = new();
    private DateTimeOffset _utcNow;

    public TestTimeProvider(DateTimeOffset? initialTime = null) {
        _utcNow = initialTime ?? DateTimeOffset.UnixEpoch;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period) {
        var timer = new TestTimer(callback, state);
        _timers.Add(timer);
        return timer;
    }

    public void Advance(TimeSpan elapsed) {
        _utcNow += elapsed;
    }

    public void FireTimers() {
        foreach (var timer in _timers.ToArray())
            timer.Fire();
    }

    private sealed class TestTimer : ITimer {
        private readonly TimerCallback _callback;
        private readonly object? _state;
        private bool _disposed;

        public TestTimer(TimerCallback callback, object? state) {
            _callback = callback;
            _state = state;
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => !_disposed;

        public void Dispose() {
            _disposed = true;
        }

        public ValueTask DisposeAsync() {
            Dispose();
            return ValueTask.CompletedTask;
        }

        public void Fire() {
            if (!_disposed)
                _callback(_state);
        }
    }
}
