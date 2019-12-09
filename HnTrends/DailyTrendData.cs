namespace HnTrends
{
    using System;
    using System.Collections.Generic;

    public class DailyTrendData
    {
        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public List<int> Counts { get; set; }

        public List<int> DailyTotals { get; set; }

        public int CountMax { get; set; }

        public List<DateTime> Dates { get; set; }
    }
}