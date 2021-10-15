namespace HnTrends.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;
    using Services;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using ViewModels;

    public class HomeController : Controller
    {
        private readonly ITrendService trendService;

        public HomeController(ITrendService trendService)
        {
            this.trendService = trendService ?? throw new ArgumentNullException(nameof(trendService));
        }

        public IActionResult Index()
        {
            var vm = new IndexViewModel
            {
                StoryCount = trendService.GetTotalStoryCount()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Trend(string id, bool allWords)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Trend search term must have a value.");
            }

            var originalTerm = id;

            id = SearchTermHelper.MakeSafeWordSearch(id, allWords);

            var resultData = await trendService.GetTrendDataForTermAsync(id);
            resultData.Term = originalTerm;

            return View(new TrendViewModel
            {
                Term = originalTerm,
                Data = JsonConvert.SerializeObject(resultData),
                To = resultData.End,
                From = resultData.Start,
                MaxCount = resultData.CountMax
            });
        }

        [HttpGet]
        public IActionResult TrendData(string term)
        {
            var data = trendService.GetTrendDataForTermAsync(term);

            return Ok(data);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
