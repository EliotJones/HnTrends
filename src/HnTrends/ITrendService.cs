namespace HnTrends
{
    using System.Threading.Tasks;

    public interface ITrendService
    {
        Task<DailyTrendData> GetTrendDataForTermAsync(string searchTerm);

        int GetTotalStoryCount();
    }
}