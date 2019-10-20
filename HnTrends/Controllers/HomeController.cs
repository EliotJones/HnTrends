using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HnTrends.Models;

namespace HnTrends.Controllers
{
    using System;
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

        [HttpPost]
        public IActionResult Trend(IndexViewModel index)
        {
            var resultData = trendService.GetTrendDataForTerm(index.Term);

            return View(new TrendViewModel
            {
                Term = index.Term,
                Data = JsonConvert.SerializeObject(resultData)
            });
        }

        [HttpGet]
        public IActionResult TrendData(string term)
        {
            var data = trendService.GetTrendDataForTerm(term);

            return Ok(data);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
