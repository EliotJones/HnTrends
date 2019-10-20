namespace HnTrends
{
    using System;

    public class TrendViewModel
    {
        public string Term { get; set; }

        public string Data { get; set; }

        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public int MaxCount { get; set; }
    }
}
