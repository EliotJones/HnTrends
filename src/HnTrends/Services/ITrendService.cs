namespace HnTrends.Services
{
    using System;
    using System.Threading.Tasks;
    using ViewModels;

    public interface ITrendService
    {
        Task<DailyTrendDataViewModel> GetTrendDataForTermAsync(string searchTerm);

        Task<FullResultsViewModel> GetFullResultsForTermAsync(string searchTerm);

        int GetTotalStoryCount();

        Task<FullResultsViewModel> GetResultsForTermInPeriodTypeBeginning(string searchTerm, GroupingType grouping, DateTime startDateInclusive);
    }
}