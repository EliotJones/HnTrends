namespace HnTrends.Controllers
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using Services;

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

        [Route("results/{term}")]
        [HttpGet]
        public ActionResult GetDataFull(string term)
        {
            return Ok(DateTime.UtcNow);
        }
    }
}
