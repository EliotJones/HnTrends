namespace HnTrends.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Caches;
    using Core;
    using Database;
    using Microsoft.Data.Sqlite;
    using ViewModels;

    internal class TrendService : ITrendService
    {
        private readonly IConnectionFactory connectionFactory;
        private readonly IPostCountsCache postCountsCache;
        private readonly IStoryCountCache storyCountCache;
        private readonly IResultsCache resultsCache;

        public TrendService(IConnectionFactory connectionFactory,
            IPostCountsCache postCountsCache,
            IStoryCountCache storyCountCache,
            IResultsCache resultsCache)
        {
            this.connectionFactory = connectionFactory;
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
                var scores = new List<int>();
                var maxCount = 0;

                for (var i = 0; i < countsByDay.Days.Count; i++)
                {
                    var date = countsByDay.Days[i];
                    scores.Add(0);
                    counts.Add(0);

                    var onDate = searchResults.Where(x => x.Date.Date == date.Date);

                    var count = 0;
                    foreach (var locatedEntry in onDate)
                    {
                        scores[i] += locatedEntry.Score;
                        count++;
                    }

                    counts[i] = (ushort)count;

                    if (count > maxCount)
                    {
                        maxCount = count;
                    }
                }

                cached = new CachedResult
                {
                    Counts = counts,
                    MaxCount = maxCount,
                    Scores = scores
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
                DailyTotals = countsByDay.PostsPerDay,
                Scores = cached.Scores
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

        public async Task<FullResultsViewModel> GetResultsForTermInPeriodTypeBeginning(string searchTerm, GroupingType grouping, DateTime startDateInclusive)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
            }

            DateTime endDate;
            switch (grouping)
            {
                case GroupingType.Day:
                    endDate = startDateInclusive.AddDays(1);
                    break;
                case GroupingType.Week:
                    endDate = startDateInclusive.AddDays(7);
                    break;
                default:
                    endDate = startDateInclusive.AddMonths(1);
                    break;
            }

            var results = await SearchFull(searchTerm, startDateInclusive, endDate);

            return new FullResultsViewModel
            {
                Start = startDateInclusive,
                DailyTotals = new List<ushort>(),
                Results = results
            };
        }

        private async Task<IReadOnlyList<LocatedEntry>> Search(string searchTerm)
        {
            var trimmedTerm = searchTerm.Trim('"');

            var termHasDotPrefix = trimmedTerm.Length > 0 && trimmedTerm[0] == '.';

            var sql = "SELECT s.time, s.score FROM search_target as st INNER JOIN story as s ON s.id = st.id WHERE st.title MATCH @query";

            if (termHasDotPrefix)
            {
                sql += " AND st.title LIKE @likeQuery;";
            }

            await using var connection = connectionFactory.Open();

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

                int score = 1;
                if (!reader.IsDBNull(1))
                {
                    score = reader.GetInt32(1);
                }

                results.Add(new LocatedEntry(Entry.TimeToDate(unixTs), score));
            }

            return results;
        }

        private async Task<IReadOnlyList<EntryWithScore>> SearchFull(string searchTerm, DateTime? startDateInclusive = null, DateTime? endDateExclusive = null)
        {
            var trimmedTerm = searchTerm.Trim('"');

            var termHasDotPrefix = trimmedTerm.Length > 0 && trimmedTerm[0] == '.';

            var sql = @"  
                    SELECT s.id, s.title, s.url, bm25(search_target), s.score, s.time FROM search_target as st
                    INNER JOIN story as s
                    ON s.id = st.id
                    WHERE st.title MATCH @query";

            if (termHasDotPrefix)
            {
                sql += " AND st.title LIKE @likeQuery";
            }

            if (startDateInclusive.HasValue)
            {
                sql += " AND s.time >= @startDate";
            }

            if (endDateExclusive.HasValue)
            {
                sql += " AND s.time < @endDate";
            }

            await using var connection = connectionFactory.Open();

            var command = new SqliteCommand(sql,
                connection);

            command.Parameters.AddWithValue("query", searchTerm);

            if (termHasDotPrefix)
            {
                command.Parameters.AddWithValue("likeQuery", $"%{trimmedTerm}%");
            }

            if (startDateInclusive.HasValue)
            {
                command.Parameters.AddWithValue("startDate", Entry.DateToTime(startDateInclusive.Value));
            }

            if (endDateExclusive.HasValue)
            {
                command.Parameters.AddWithValue("endDate", Entry.DateToTime(endDateExclusive.Value));
            }

            await using var reader = await command.ExecuteReaderAsync();

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
                var rank = reader.GetDouble(3);
                var score = 1;
                if (!reader.IsDBNull(4))
                {
                    score = reader.GetInt32(4);
                }

                var time = reader.GetInt64(5);

                results.Add(new EntryWithScore
                {
                    Id = id,
                    Rank = rank,
                    Title = title,
                    Url = url,
                    Score = score,
                    Time = Entry.TimeToDate(time)
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

        public int Score { get; set; }

        public LocatedEntry(DateTime date, int score)
        {
            Date = date;
            Score = score;
        }
    }
}
