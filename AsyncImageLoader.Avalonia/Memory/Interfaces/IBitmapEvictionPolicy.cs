using AsyncImageLoader.Memory.Services;

namespace AsyncImageLoader.Memory.Interfaces;

public interface IBitmapEvictionPolicy
{
    bool ShouldEvict(BitmapEntry value);
}