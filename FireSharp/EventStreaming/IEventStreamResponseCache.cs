using System;
using System.Threading.Tasks;

namespace FireSharp.EventStreaming
{
    public interface IEventStreamResponseCache<T>
    {
        Task<T> Get(string path);

        Task AddOrUpdate(string path, T data);

        Task Remove(string path);

        Task RemoveAllAsync();
    }
}