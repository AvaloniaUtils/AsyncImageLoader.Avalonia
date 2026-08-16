using System;
using System.Threading;
using AsyncImageLoader.Core.Leases;

namespace AsyncImageLoader.Core.Pipeline;

internal sealed class ImageRequestCoordinator : IDisposable {
    private readonly object _gate = new();
    private CancellationTokenSource? _cancellation;
    private IImageLease? _lease;
    private long _generation;
    private bool _disposed;

    public Request Begin() {
        CancellationTokenSource? cancellation;
        IImageLease? lease;
        Request request;
        lock (_gate) {
            ThrowIfDisposed();
            cancellation = DetachCancellationLocked();
            lease = DetachLeaseLocked();

            _cancellation = new CancellationTokenSource();
            _generation++;
            request = new Request(_generation, _cancellation.Token);
        }

        ReleaseResources(cancellation, lease);
        return request;
    }

    public bool TrySetLease(Request request, IImageLease? lease) {
        IImageLease? previousLease = null;
        var accepted = false;
        lock (_gate) {
            if (!_disposed && IsCurrentLocked(request)) {
                previousLease = DetachLeaseLocked();
                _lease = lease;
                accepted = true;
            }
        }

        if (accepted)
            SafeDispose(previousLease);
        else
            SafeDispose(lease);

        return accepted;
    }

    public bool TryComplete(Request request) {
        CancellationTokenSource? cancellation = null;
        var completed = false;
        lock (_gate) {
            if (!_disposed && IsCurrentLocked(request)) {
                cancellation = DetachCancellationLocked();
                completed = true;
            }
        }

        cancellation?.Dispose();
        return completed;
    }

    public void Cancel() {
        CancellationTokenSource? cancellation;
        IImageLease? lease;
        lock (_gate) {
            if (_disposed)
                return;

            _generation++;
            cancellation = DetachCancellationLocked();
            lease = DetachLeaseLocked();
        }

        ReleaseResources(cancellation, lease);
    }

    public void Dispose() {
        CancellationTokenSource? cancellation;
        IImageLease? lease;
        lock (_gate) {
            if (_disposed)
                return;

            _disposed = true;
            _generation++;
            cancellation = DetachCancellationLocked();
            lease = DetachLeaseLocked();
        }

        ReleaseResources(cancellation, lease);
    }

    private bool IsCurrentLocked(Request request) {
        return _cancellation is not null &&
               request.Generation == _generation &&
               !request.CancellationToken.IsCancellationRequested;
    }

    private CancellationTokenSource? DetachCancellationLocked() {
        var cancellation = _cancellation;
        _cancellation = null;
        return cancellation;
    }

    private IImageLease? DetachLeaseLocked() {
        var lease = _lease;
        _lease = null;
        return lease;
    }

    private static void CancelAndDispose(CancellationTokenSource? cancellation) {
        if (cancellation is null)
            return;

        try {
            cancellation.Cancel();
        }
        finally {
            cancellation.Dispose();
        }
    }

    private static void ReleaseResources(CancellationTokenSource? cancellation, IImageLease? lease) {
        try {
            CancelAndDispose(cancellation);
        }
        catch {
            // Cancellation callbacks are external code and must not prevent lease release.
        }

        SafeDispose(lease);
    }

    private static void SafeDispose(IImageLease? lease) {
        try {
            lease?.Dispose();
        }
        catch {
            // Release callbacks are external code and must not break coordinator state.
        }
    }

    private void ThrowIfDisposed() {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImageRequestCoordinator));
    }

    internal readonly record struct Request(long Generation, CancellationToken CancellationToken);
}
