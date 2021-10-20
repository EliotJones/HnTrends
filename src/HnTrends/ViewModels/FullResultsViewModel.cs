namespace HnTrends.ViewModels
{
    using System;
    using System.Collections.Generic;

    public class FullResultsViewModel
    {
        public DateTime Start { get; set; }

        public List<ushort> DailyTotals { get; set; }

        public IReadOnlyList<EntryWithScore> Results { get; set; }
    }

    public class EntryWithScore
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public double Rank { get; set; }

        public int Score { get; set; }
    }
}