namespace HnTrends.Caches
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SQLite;
    using Database;
    using Microsoft.Extensions.Caching.Memory;

    internal class PostCountsCache : IPostCountsCache
    {
        private static readonly object Lock = new object();

        private readonly IMemoryCache memoryCache;
        private readonly SQLiteConnection connection;

        public PostCountsCache(IMemoryCache memoryCache, ICacheManager cacheManager, SQLiteConnection connection)
        {
            this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            
            if (this.connection.State == ConnectionState.Closed)
            {
                throw new ArgumentException("Connection was closed.", nameof(connection));
            }

            cacheManager.Register(nameof(PostCountsByDay));
        }

        public PostCountsByDay Get()
        {
            lock (Lock)
            {
                if (memoryCache.TryGetValue(nameof(PostCountsByDay), out PostCountsByDay cachedResult))
                {
                    return cachedResult;
                }

                if (!DateRangeTable.TryRead(connection, out var range))
                {
                    throw new InvalidOperationException("Empty date range table in SQLite database.");
                }

                var min = range.from;
                var max = range.to;

                var totalsByDay = new Dictionary<DateTime, ushort>();

                foreach (var entry in StoryTable.GetEntries(connection))
                {
                    var day = entry.Date.Date;

                    if (!totalsByDay.ContainsKey(day))
                    {
                        totalsByDay[day] = 1;
                    }
                    else
                    {
                        totalsByDay[day]++;
                    }
                }

                var days = new List<DateTime>();
                var counts = new List<ushort>();

                var current = min;
                while (current.Date < max.Date)
                {
                    days.Add(current.Date);

                    if (totalsByDay.TryGetValue(current.Date, out var count))
                    {
                        counts.Add(count);
                    }
                    else
                    {
                        counts.Add(0);
                    }

                    current = current.AddDays(1);
                }

                cachedResult = new PostCountsByDay(min, max, counts, days);

                memoryCache.Set(nameof(PostCountsByDay), cachedResult);

                return cachedResult;
            }
        }
    }
}