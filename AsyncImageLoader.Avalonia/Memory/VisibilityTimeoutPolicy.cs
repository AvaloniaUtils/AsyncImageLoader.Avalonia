using System;
using AsyncImageLoader.Memory.Interfaces;
using AsyncImageLoader.Memory.Services;

namespace AsyncImageLoader.Memory;

public class VisibilityTimeoutPolicy : IBitmapEvictionPolicy
{
    private readonly TimeSpan _timeout;
    
    public  VisibilityTimeoutPolicy(TimeSpan timeout) 
        => _timeout = timeout;

    public bool ShouldEvict(BitmapEntry entry) {
        return entry.RefCount == 0 &&
               entry.LastReleased + _timeout <= DateTime.UtcNow;
    }
}