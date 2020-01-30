namespace HnTrends.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Caches;
    using Indexer;
    using Microsoft.CodeAnalysis.Operations;
    using ViewModels;

    internal class TrendService : ITrendService
    {
        private readonly IIndexManager indexManager;
        private readonly IPostCountsCache postCountsCache;
        private readonly IStoryCountCache storyCountCache;

        public TrendService(IIndexManager indexManager,
            IPostCountsCache postCountsCache,
            IStoryCountCache storyCountCache)
        {
            this.indexManager = indexManager ?? throw new ArgumentNullException(nameof(indexManager));
            this.postCountsCache = postCountsCache ?? throw new ArgumentNullException(nameof(postCountsCache));
            this.storyCountCache = storyCountCache ?? throw new ArgumentNullException(nameof(storyCountCache));
        }

        public Task<DailyTrendDataViewModel> GetTrendDataForTermAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term cannot be null or empty", nameof(searchTerm));
            }

            var countsByDay = postCountsCache.Get();

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
            
            var result = new DailyTrendDataViewModel
            {
                Counts = counts,
                Start = countsByDay.Min,
                End = countsByDay.Max,
                CountMax = maxCount,
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
