namespace HnTrends.Caches
{
    using System.Collections.Generic;

    public class CachedResult
    {
        public List<ushort> Counts { get; set; }

        public int MaxCount { get; set; }
    }
}