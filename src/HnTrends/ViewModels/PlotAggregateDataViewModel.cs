namespace HnTrends.ViewModels
{
    using System.Collections.Generic;

    public class PlotAggregateDataViewModel
    {
        public List<ushort> Counts { get; set; }

        public List<int> Scores { get; set; }

        public string Term { get; set; }

        public bool AllWords { get; set; }
    }
}
