namespace HnTrends.Caches
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Caching.Memory;

    internal class CacheManager : ICacheManager
    {
        private readonly object mutex = new object();
        private readonly HashSet<string> keys = new HashSet<string>();

        private readonly IMemoryCache memoryCache;

        public CacheManager(IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache 
                               ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public void Register(string key)
        {
            lock (mutex)
            {
                keys.Add(key);
            }
        }

        public void Clear()
        {
            lock (mutex)
            {
                foreach (var key in keys)
                {
                    try
                    {
                        memoryCache.Remove(key);
                    }
                    catch
                    {
                        // ignored.
                    }
                }
            }
        }
    }
}