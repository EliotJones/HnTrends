namespace HnTrends.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Caches;
    using Indexer;
    using ViewModels;

    internal class TrendService : ITrendService
    {
        private readonly IIndexManager indexManager;
        private readonly IPostCountsCache postCountsCache;
        private readonly IStoryCountCache storyCountCache;
        private readonly IResultsCache resultsCache;

        public TrendService(IIndexManager indexManager,
            IPostCountsCache postCountsCache,
            IStoryCountCache storyCountCache,
            IResultsCache resultsCache)
        {
            this.indexManager = indexManager ?? throw new ArgumentNullException(nameof(indexManager));
            this.postCountsCache = postCountsCache ?? throw new ArgumentNullException(nameof(postCountsCache));
            this.storyCountCache = storyCountCache ?? throw new ArgumentNullException(nameof(storyCountCache));
            this.resultsCache = resultsCache ?? throw new ArgumentNullException(nameof(resultsCache));
        }

        public Task<DailyTrendDataViewModel> GetTrendDataForTermAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
            }

            var countsByDay = postCountsCache.Get();

            if (!resultsCache.TryGet(searchTerm, out var cached))
            {
                var searchResults = indexManager.Search(searchTerm);

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

            return Task.FromResult(result);
        }

        public Task<FullResultsViewModel> GetFullResultsForTermAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
            }

            var postCounts = postCountsCache.Get();

            var fullSearchResults = indexManager.SearchWithFullResults(searchTerm);

            return Task.FromResult(new FullResultsViewModel
            {
                Start = postCounts.Min,
                DailyTotals = postCounts.PostsPerDay,
                Results = fullSearchResults.ToList()
            });
        }

        public int GetTotalStoryCount()
        {
            return storyCountCache.Get();
        }
    }
}
