namespace HnTrends
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Data.SQLite;
    using Microsoft.Extensions.Caching.Memory;

    internal class PostCountsCache : IPostCountsCache
    {
        private static readonly object Lock = new object();

        private readonly IMemoryCache memoryCache;
        private readonly SQLiteConnection connection;

        public PostCountsCache(IMemoryCache memoryCache,
            SQLiteConnection connection)
        {
            this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));

            if (this.connection.State == ConnectionState.Closed)
            {
                throw new ArgumentException("Connection was closed.", nameof(connection));
            }
        }

        public PostCountsByDay Get()
        {
            lock (Lock)
            {
                if (memoryCache.TryGetValue(nameof(PostCountsByDay), out PostCountsByDay cachedResult))
                {
                    return cachedResult;
                }

                var minmaxCommand = new SQLiteCommand("SELECT MIN(ticks), MAX(ticks) FROM story;", connection);

                var min = DateTime.MinValue;
                var max = DateTime.MaxValue;
                using (var minmaxReader = minmaxCommand.ExecuteReader())
                {
                    while (minmaxReader.Read())
                    {
                        min = GetDateFromReader(minmaxReader, 0);
                        max = GetDateFromReader(minmaxReader, 1);
                        break;
                    }
                }

                var totalsCommand =
                    new SQLiteCommand(@"SELECT ticks FROM story WHERE title IS NOT NULL AND url IS NOT NULL;",
                        connection);

                var totalsByDay = new Dictionary<DateTime, ushort>();

                using (var totalsReader = totalsCommand.ExecuteReader())
                {
                    while (totalsReader.Read())
                    {
                        var day = GetDateFromReader(totalsReader, 0).Date;

                        if (!totalsByDay.ContainsKey(day))
                        {
                            totalsByDay[day] = 1;
                        }
                        else
                        {
                            totalsByDay[day]++;
                        }
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
                
                cachedResult = new PostCountsByDay(min, max, counts, days );

                memoryCache.Set(nameof(PostCountsByDay), cachedResult);

                return cachedResult;
            }
        }

        private static DateTime GetDateFromReader(DbDataReader reader, int ordinal)
        {
            return new DateTime(reader.GetInt64(ordinal));
        }
    }
}