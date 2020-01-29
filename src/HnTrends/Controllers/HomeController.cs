using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HnTrends.Models;

namespace HnTrends.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class HomeController : Controller
    {
        private readonly ITrendService trendService;

        public HomeController(ITrendService trendService)
        {
            this.trendService = trendService ?? throw new ArgumentNullException(nameof(trendService));
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Trend(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Trend search term must have a value.");
            }

            var resultData = await trendService.GetTrendDataForTermAsync(id);

            return View(new TrendViewModel
            {
                Term = id,
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
