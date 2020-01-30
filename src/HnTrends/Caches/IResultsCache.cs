namespace HnTrends.Caches
{
    internal interface IResultsCache
    {
        void Cache(string searchTerm, CachedResult result);
        bool TryGet(string searchTerm, out CachedResult cached);
    }
}