using System;
using System.Threading.Tasks;

using Provision.Interfaces;
using Provision.Providers.PortableMemoryCache;

namespace FireSharp.EventStreaming
{
    public class InMemoryEntityResponseCache<T> : IEventStreamResponseCache<T>, IDisposable
    {
        private readonly ICacheHandler _cacheHandler;

        public InMemoryEntityResponseCache()
        {
            _cacheHandler = new PortableMemoryCacheHandler(new PortableMemoryCacheHandlerConfiguration(TimeSpan.FromMinutes(15)));
        } 

        public async Task<T> Get(string path)
        {
            var key = _cacheHandler.CreateKey(path);

            return await _cacheHandler.GetValue<T>(key);
        }

        public async Task AddOrUpdate(string path, T data)
        {
            var key = _cacheHandler.CreateKey(path);
            await _cacheHandler.AddOrUpdate(key, data);
        }

        public async Task Remove(string path)
        {
            var key = _cacheHandler.CreateKey(path);

            await _cacheHandler.RemoveByKey(key);
        }

        public async Task RemoveAllAsync()
        {
            await _cacheHandler.Purge();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_cacheHandler != null)
                {
                    _cacheHandler.Purge();
                }
            }
        }
    }
}