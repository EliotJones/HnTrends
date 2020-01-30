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

        public TrendService(IIndexManager indexManager,
            IPostCountsCache postCountsCache,
            IStoryCountCache storyCountCache)
        {
            this.indexManager = indexManager ?? throw new ArgumentNullException(nameof(indexManager));
            this.postCountsCache = postCountsCache ?? throw new ArgumentNullException(nameof(postCountsCache));
            this.storyCountCache = storyCountCache ?? throw new ArgumentNullException(nameof(storyCountCache));
        }

        public Task<DailyTrendData> GetTrendDataForTermAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException();
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
            
            var result = new DailyTrendData
            {
                Counts = counts,
                Dates = countsByDay.Days,
                Start = countsByDay.Min,
                End = countsByDay.Max,
                CountMax = maxCount,
                DailyTotals = countsByDay.PostsPerDay
            };

            return Task.FromResult(result);
        }

        public int GetTotalStoryCount()
        {
            return storyCountCache.Get();
        }
    }
}
