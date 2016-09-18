using System;
using System.Linq;

using FireSharp.EventStreaming;

using Provision.Providers.PortableMemoryCache.Mono;

namespace FireSharp
{
    public class InMemoryEventStreamCacheProvider : IEventStreamCacheProvider
    {
        #region Fields

        private readonly ConcurrentDictionary<string, object> _cacheTable;

        #endregion

        #region Constructors and Destructors

        public InMemoryEventStreamCacheProvider()
        {
            _cacheTable = new ConcurrentDictionary<string, object>();
        }

        #endregion

        #region Public Methods and Operators

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IEventStreamResponseCache<T> GetCache<T>(string basePath)
        {
            var key = $"{typeof(T).FullName}_{basePath}}}";

            return (IEventStreamResponseCache<T>)_cacheTable.GetOrAdd(key, t => new InMemoryEntityResponseCache<T>(basePath));
        }

        #endregion

        #region Methods

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_cacheTable.Any())
                {
                    var caches = _cacheTable.Select(pair => pair.Value).Cast<IDisposable>().ToList();
                    foreach (var cache in caches)
                    {
                        cache.Dispose();
                    } 
                }
            }
        }

        #endregion
    }
}