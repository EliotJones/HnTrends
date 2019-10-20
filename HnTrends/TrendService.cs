namespace HnTrends
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.SQLite;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;

    public class TrendService : ITrendService, IDisposable
    {
        private static readonly object Locker = new object();

        private (DateTime min, DateTime max)? minmaxcache;

        private readonly IMemoryCache cache;
        private readonly SQLiteConnection connection;

        public TrendService(IOptions<FileLocations> fileLocationOptions, IMemoryCache cache)
        {
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
            connection = new SQLiteConnection($"Data Source={fileLocationOptions.Value.Database}");
            connection.Open();
        }

        public async Task<DailyTrendData> GetTrendDataForTermAsync(string searchTerm)
        {
            DateTime GetDateFromReader(DbDataReader reader, int ordinal)
            {
                return new DateTime(reader.GetInt64(ordinal));
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException();
            }

            Dictionary<DateTime, int> totalsByDay;

            lock (Locker)
            {
                if (cache.TryGetValue(searchTerm.ToLowerInvariant(), out DailyTrendData cached))
                {
                    return cached;
                }

                if (!minmaxcache.HasValue)
                {
                    var minmaxCommand = new SQLiteCommand("SELECT MIN(ticks), MAX(ticks) FROM story;", connection);

                    DateTime min = DateTime.MinValue;
                    DateTime max = DateTime.MaxValue;
                    using (var minmaxReader = minmaxCommand.ExecuteReader())
                    {
                        while (minmaxReader.Read())
                        {
                            min = GetDateFromReader(minmaxReader, 0);
                            max = GetDateFromReader(minmaxReader, 1);
                            break;
                        }
                    }

                    minmaxcache = (min, max);
                }

                if (!cache.TryGetValue("daytotals", out totalsByDay))
                {
                    var totalsCommand = 
                        new SQLiteCommand(@"SELECT ticks FROM story WHERE title IS NOT NULL AND url IS NOT NULL;", 
                            connection);

                    totalsByDay = new Dictionary<DateTime, int>();

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

                    cache.Set("daytotals", totalsByDay);
                }
            }

            var urls = new HashSet<string>();
            var counts = new Dictionary<DateTime, int>();

            // TODO: SQLite nocase only supports ASCII.
            var command = new SQLiteCommand(@"  SELECT url, ticks 
                                                FROM story 
                                                WHERE title IS NOT NULL
                                                AND url IS NOT NULL 
                                                AND (title LIKE @title OR url LIKE @url) 
                                                ORDER BY ticks;", connection);

            command.Parameters.AddWithValue("title", $"%{searchTerm}%");
            command.Parameters.AddWithValue("url", $"%{searchTerm}%");

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    var url = reader.GetString(0);
                    var date = new DateTime(reader.GetInt64(1));

                    if (urls.Contains(url))
                    {
                        continue;
                    }

                    urls.Add(url);
                    
                    if (!counts.ContainsKey(date.Date))
                    {
                        counts[date.Date] = 1;
                    }
                    else
                    {
                        counts[date.Date]++;
                    }
                }
            }
            
            var countMax = 0;
            var countsNum = new List<int>(counts.Count);
            var dates = new List<DateTime>(counts.Count);
            var percents = new List<double>();
            foreach (var pair in counts.OrderBy(x => x.Key))
            {
                countsNum.Add(pair.Value);
                dates.Add(pair.Key);
                percents.Add((pair.Value / (double)totalsByDay[pair.Key]) * 100.0);

                if (pair.Value > countMax)
                {
                    countMax = pair.Value;
                }
            }

            var result = new DailyTrendData
            {
                Counts = countsNum,
                Dates = dates,
                Start = minmaxcache.Value.min,
                End = minmaxcache.Value.max,
                CountMax = countMax,
                Percents = percents
            };

            lock (Locker)
            {
                cache.Set(searchTerm.ToLowerInvariant(), result);
            }

            return result;
        }
        
        public void Dispose()
        {
            connection.Dispose();
        }
    }
}
