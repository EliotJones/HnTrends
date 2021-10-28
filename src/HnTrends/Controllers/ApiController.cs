namespace HnTrends.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Services;
    using System;
    using System.Net;
    using ViewModels;

    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly ITrendService trendService;

        public ApiController(ITrendService trendService)
        {
            this.trendService = trendService;
        }

        [HttpGet]
        public ActionResult Index()
        {
            return Ok(trendService.GetTotalStoryCount());
        }

        [Route("single/{grouping}/{date}/{term}")]
        [HttpGet]
        public async Task<ActionResult> GetSingleDataPointByGrouping(string grouping, string date, string term, [FromQuery] bool allWords)
        {
            term = WebUtility.UrlDecode(term);

            term = SearchTermHelper.MakeSafeWordSearch(term, allWords);

            if (!DateTime.TryParse(date, out var dateActual))
            {
                return BadRequest($"unrecognized date value: {date}.");
            }

            var results = await trendService.GetResultsForTermInPeriodTypeBeginning(term, ParseGrouping(grouping), dateActual);

            return Ok(results);
        }

        [Route("results/{term}")]
        [HttpGet]
        public async Task<ActionResult> GetDataFull(string term, [FromQuery] bool allWords)
        {
            term = WebUtility.UrlDecode(term);

            term = SearchTermHelper.MakeSafeWordSearch(term, allWords);

            var full = await trendService.GetFullResultsForTermAsync(term);

            return Ok(full);
        }

        [Route("plot/{term}")]
        [HttpGet]
        public async Task<ActionResult> GetPlotAggregateData(string term, [FromQuery] bool allWords)
        {
            term = WebUtility.UrlDecode(term);

            var originalTerm = term;

            term = SearchTermHelper.MakeSafeWordSearch(term, allWords);

            var results = await trendService.GetTrendDataForTermAsync(term);

            return Ok(new PlotAggregateDataViewModel
            {
                Counts = results.Counts,
                Term = originalTerm,
                AllWords = allWords,
                Scores = results.Scores
            });
        }

        private static GroupingType ParseGrouping(string value)
        {
            if (value?.IndexOf("day", StringComparison.OrdinalIgnoreCase) >= 0
            || value?.IndexOf("daily", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return GroupingType.Day;
            }

            if (value?.IndexOf("week", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return GroupingType.Week;
            }

            return GroupingType.Month;
        }
    }
}
