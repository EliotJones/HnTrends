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
        private static readonly char[] SplitChars = {' '};

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

            if (allWords)
            {
                id = MakeAllWordSearch(id);
            }

            var resultData = await trendService.GetTrendDataForTermAsync(id);

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

        private static string MakeAllWordSearch(string id)
        {
            if (id.IndexOf(' ') < 0)
            {
                return id;
            }

            var parts = id.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);

            var result = string.Empty;
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part.Length > 1 && part[0] != '+' && part[0] != '-')
                {
                    result += '+';
                }

                result += part;

                if (i < parts.Length - 1)
                {
                    result += ' ';
                }
            }

            return result;
        }
    }
}
