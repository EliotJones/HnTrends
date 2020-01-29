namespace HnTrends.Caches
{
    using System;
    using System.Collections.Generic;

    internal class PostCountsByDay
    {
        public DateTime Min { get; }

        public DateTime Max { get; }

        public List<ushort> PostsPerDay { get; }

        public List<DateTime> Days { get; }

        public PostCountsByDay(DateTime min, DateTime max, List<ushort> postsPerDay, List<DateTime> days)
        {
            Min = min;
            Max = max;
            PostsPerDay = postsPerDay ?? throw new ArgumentNullException(nameof(postsPerDay));
            Days = days ?? throw new ArgumentNullException(nameof(days));
        }
    }
}
