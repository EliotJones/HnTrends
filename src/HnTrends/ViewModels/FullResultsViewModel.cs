namespace HnTrends.ViewModels
{
    using System;
    using System.Collections.Generic;
    using Indexer;

    public class FullResultsViewModel
    {
        public DateTime Start { get; set; }

        public List<ushort> DailyTotals { get; set; }

        public List<EntryWithScore> Results { get; set; }
    }
}