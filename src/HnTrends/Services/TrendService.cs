namespace HnTrends.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Caches;
    using Core;
    using Microsoft.Data.Sqlite;
    using ViewModels;

    internal class TrendService : ITrendService
    {
        private readonly SqliteConnection connection;
        private readonly IPostCountsCache postCountsCache;
        private readonly IStoryCountCache storyCountCache;
        private readonly IResultsCache resultsCache;

        public TrendService(SqliteConnection connection,
            IPostCountsCache postCountsCache,
            IStoryCountCache storyCountCache,
            IResultsCache resultsCache)
        {
            this.connection = connection;
            this.postCountsCache = postCountsCache ?? throw new ArgumentNullException(nameof(postCountsCache));
            this.storyCountCache = storyCountCache ?? throw new ArgumentNullException(nameof(storyCountCache));
            this.resultsCache = resultsCache ?? throw new ArgumentNullException(nameof(resultsCache));
        }

        public async Task<DailyTrendDataViewModel> GetTrendDataForTermAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
            }

            var countsByDay = postCountsCache.Get();

            if (!resultsCache.TryGet(searchTerm, out var cached))
            {
                var searchResults = await Search(searchTerm);

                var counts = new List<ushort>();
                var maxCount = 0;

                for (var i = 0; i < countsByDay.Days.Count; i++)
                {
                    var date = countsByDay.Days[i];

                    var onDate = searchResults.Count(x => x.Date.Date == date.Date);

                    counts.Add((ushort)onDate);

                    if (onDate > maxCount)
                    {
                        maxCount = onDate;
                    }
                }

                cached = new CachedResult
                {
                    Counts = counts,
                    MaxCount = maxCount
                };

                if (searchResults.Count > 1000)
                {
                    resultsCache.Cache(searchTerm, cached);
                }
            }

            var result = new DailyTrendDataViewModel
            {
                Counts = cached.Counts,
                Start = countsByDay.Min,
                End = countsByDay.Max,
                CountMax = cached.MaxCount,
                DailyTotals = countsByDay.PostsPerDay
            };

            return result;
        }

        public async Task<FullResultsViewModel> GetFullResultsForTermAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
            }

            var postCounts = postCountsCache.Get();

            var results = await SearchFull(searchTerm);

            return new FullResultsViewModel
            {
                Start = postCounts.Min,
                DailyTotals = postCounts.PostsPerDay,
                Results = results
            };
        }

        private async Task<IReadOnlyList<LocatedEntry>> Search(string searchTerm)
        {
            var trimmedTerm = searchTerm.Trim('"');

            var termHasDotPrefix = trimmedTerm.Length > 0 && trimmedTerm[0] == '.';

            var sql = "SELECT time FROM search_target WHERE title MATCH @query";

            if (termHasDotPrefix)
            {
                sql += " AND title LIKE @likeQuery;";
            }

            var command = new SqliteCommand(sql,
                connection);

            command.Parameters.AddWithValue("query", searchTerm);

            if (termHasDotPrefix)
            {
                command.Parameters.AddWithValue("likeQuery", $"%{trimmedTerm}%");
            }

            using var reader = await command.ExecuteReaderAsync();

            var results = new List<LocatedEntry>();
            while (reader.Read())
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                var unixTs = reader.GetInt64(0);

                results.Add(new LocatedEntry(Entry.TimeToDate(unixTs)));
            }

            return results;
        }

        private async Task<IReadOnlyList<EntryWithScore>> SearchFull(string searchTerm)
        {
            var trimmedTerm = searchTerm.Trim('"');

            var termHasDotPrefix = trimmedTerm.Length > 0 && trimmedTerm[0] == '.';

            var sql = @"  
                    SELECT s.id, s.title, s.url, bm25(search_target) FROM search_target as st
                    INNER JOIN story as s
                    ON s.id = st.id
                    WHERE st.title MATCH @query";

            if (termHasDotPrefix)
            {
                sql += " AND st.title LIKE @likeQuery";
            }

            var command = new SqliteCommand(sql,
                connection);

            command.Parameters.AddWithValue("query", searchTerm);

            if (termHasDotPrefix)
            {
                command.Parameters.AddWithValue("likeQuery", $"%{trimmedTerm}%");
            }

            using var reader = await command.ExecuteReaderAsync();

            var results = new List<EntryWithScore>();
            while (reader.Read())
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                var id = reader.GetInt32(0);
                var title = reader.GetString(1);
                var url = reader.GetString(2);
                var score = reader.GetDouble(3);

                results.Add(new EntryWithScore
                {
                    Id = id,
                    Score = score,
                    Title = title,
                    Url = url
                });
            }

            return results;
        }

        public int GetTotalStoryCount()
        {
            return storyCountCache.Get();
        }
    }

    internal class LocatedEntry
    {
        public DateTime Date { get; }

        public LocatedEntry(DateTime date)
        {
            Date = date;
        }
    }
}
