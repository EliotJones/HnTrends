namespace HnTrends.ViewModels
{
    using System;
    using System.Collections.Generic;

    public class DailyTrendDataViewModel
    {
        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public List<ushort> Counts { get; set; }

        public List<ushort> DailyTotals { get; set; }

        public int CountMax { get; set; }

        public string Term { get; set; }
    }
}