namespace HnTrends.Services
{
    using System.Threading.Tasks;
    using ViewModels;

    public interface ITrendService
    {
        Task<DailyTrendData> GetTrendDataForTermAsync(string searchTerm);

        int GetTotalStoryCount();
    }
}