namespace HnTrends.Caches
{
    using System;
    using Database;
    using Microsoft.Extensions.Caching.Memory;

    internal class StoryCountCache : IStoryCountCache
    {
        private static readonly object Lock = new object();

        private readonly IMemoryCache memoryCache;
        private readonly IConnectionFactory connectionFactory;

        public StoryCountCache(IMemoryCache memoryCache, ICacheManager cacheManager, IConnectionFactory connectionFactory)
        {
            this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            this.connectionFactory = connectionFactory;

            cacheManager.Register(nameof(StoryCountCache));
        }

        public int Get()
        {
            lock (Lock)
            {
                if (memoryCache.TryGetValue(nameof(StoryCountCache), out int value))
                {
                    return value;
                }

                using var connection = connectionFactory.Open();

                value = StoryTable.GetCount(connection);

                memoryCache.Set(nameof(StoryCountCache), value);

                return value;
            }
        }
    }
}
