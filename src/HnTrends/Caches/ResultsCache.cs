namespace HnTrends.Caches
{
    using System;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;

    internal class ResultsCache : IResultsCache
    {
        private readonly IMemoryCache cache;
        private readonly ICacheManager cacheManager;
        private readonly ILogger<ResultsCache> logger;

        private readonly object mutex = new object();

        public ResultsCache(IMemoryCache cache, ICacheManager cacheManager, ILogger<ResultsCache> logger)
        {
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this.cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Cache(string searchTerm, CachedResult result)
        {
            lock (mutex)
            {
                cache.Set(searchTerm, result);
                cacheManager.Register(searchTerm);
            }
        }

        public bool TryGet(string searchTerm, out CachedResult cached)
        {
            cached = null;

            lock (mutex)
            {
                if (cache.TryGetValue(searchTerm, out CachedResult result))
                {
                    logger.LogInformation($"Using cached value for: {searchTerm}.");

                    cached = result;
                    return true;
                }
            }

            return false;
        }
    }
}
