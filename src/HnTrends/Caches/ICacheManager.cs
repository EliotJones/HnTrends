namespace HnTrends.Caches
{
    internal interface ICacheManager
    {
        void Register(string key);

        void Clear();
    }
}
