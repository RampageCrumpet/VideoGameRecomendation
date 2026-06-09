using GameRecommendation.API.DataTransferObjects.Common;
using GameRecommendation.API.DataTransferObjects.Recommendations;
using GameRecommendation.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameRecommendation.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RecommendationsController : ControllerBase
    {
        private readonly IRecommendationService recommendationService;

        public RecommendationsController(IRecommendationService recommendationService)
        {
            this.recommendationService = recommendationService;
        }

        /// <summary>
        /// Returns a paginated list of game recommendations for the calling user.
        /// </summary>
        /// <param name="page">The one-based page number. Defaults to 1.</param>
        /// <param name="pageSize">The number of recommendations per page. Defaults to 20.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<RecommendationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRecommendations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await recommendationService.GetRecommendationsAsync(userId, page, pageSize);

            return Ok(new PagedResultDto<RecommendationDto>
            {
                Items = result.Items.Select(r => new RecommendationDto
                {
                    Id = r.Game.Id,
                    Name = r.Game.Name,
                    ImageUrl = r.Game.ImageUrl,
                    ReleaseDate = r.Game.ReleaseDate,
                    Score = r.Score
                }),
                Page = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount
            });
        }
    }
}