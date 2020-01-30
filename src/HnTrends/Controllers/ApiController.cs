namespace HnTrends.Controllers
{
    using System.Threading.Tasks;
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
        public async Task<ActionResult> GetDataFull(string term)
        {
            var full = await trendService.GetFullResultsForTermAsync(term)
                .ConfigureAwait(false);

            return Ok(full);
        }
    }
}
