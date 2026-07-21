using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceApotheke.API.Services.Workers;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication (Pharmacist or Pharmacy)
    public class NewsController : ControllerBase
    {
        private readonly NewsRssService _newsService;

        public NewsController(NewsRssService newsService)
        {
            _newsService = newsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNews(CancellationToken cancellationToken)
        {
            var news = await _newsService.GetLatestNewsAsync(cancellationToken);
            return Ok(news);
        }
    }
}
