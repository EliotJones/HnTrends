namespace HnTrends.Services
{
    using System.Threading.Tasks;
    using ViewModels;

    public interface ITrendService
    {
        Task<DailyTrendDataViewModel> GetTrendDataForTermAsync(string searchTerm);

        Task<FullResultsViewModel> GetFullResultsForTermAsync(string searchTerm);

        int GetTotalStoryCount();
    }
}