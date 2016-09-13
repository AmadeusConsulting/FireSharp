using System;

using FireSharp.EventStreaming;

namespace FireSharp
{
    public interface IEventStreamCacheProvider : IDisposable
    {
        IEventStreamResponseCache<T> GetCache<T>();
    }
}