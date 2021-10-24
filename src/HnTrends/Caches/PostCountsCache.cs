namespace HnTrends.Caches
{
    using Core;
    using System;
    using System.Collections.Generic;
    using Database;
    using Microsoft.Data.Sqlite;
    using Microsoft.Extensions.Caching.Memory;

    internal class PostCountsCache : IPostCountsCache
    {
        private static readonly object Lock = new object();

        private readonly IMemoryCache memoryCache;
        private readonly IConnectionFactory connectionFactory;

        public PostCountsCache(IMemoryCache memoryCache, ICacheManager cacheManager, IConnectionFactory connectionFactory)
        {
            this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            this.connectionFactory = connectionFactory;

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

                using var connection = connectionFactory.Open();

                const int minTimestamp = 1160418111;

                var sql = $@"
                    WITH RECURSIVE
                        dateseq(x) AS
                        (
                            SELECT 0
                            UNION ALL
                            SELECT x+1 FROM dateseq
                            LIMIT 
                            (
                                SELECT ((julianday('now', 'start of day') - julianday(DATE({minTimestamp}, 'unixepoch')))) + 1
                            )
                      )
                    SELECT  d.startday,
                            (SELECT COUNT(*) FROM story WHERE time >= d.startday AND time < d.endday) as storycount
                    FROM 
                    (
                        SELECT  strftime('%s', date(julianday(DATE({minTimestamp}, 'unixepoch')), '+' || x || ' days')) as startday,
                                strftime('%s', date(julianday(DATE({minTimestamp}, 'unixepoch')), '+' || (x+1) || ' days')) as endday
                        FROM dateseq
                    ) as d;";

                var days = new List<DateTime>();
                var counts = new List<ushort>();

                using var query = new SqliteCommand(sql, connection);

                using (var reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0))
                        {
                            continue;
                        }

                        days.Add(Entry.TimeToDate(reader.GetInt64(0)));
                        counts.Add((ushort)reader.GetInt32(1));
                    }
                }

                cachedResult = new PostCountsByDay(days[0], days[^1], counts, days);

                memoryCache.Set(nameof(PostCountsByDay), cachedResult);

                return cachedResult;
            }
        }
    }
}