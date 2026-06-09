using GameRecommendation.API.DataTransferObjects.Common;
using GameRecommendation.API.DataTransferObjects.Games;
using GameRecommendation.Domain.Enums;
using GameRecommendation.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameRecommendation.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GamesController : ControllerBase
    {
        private readonly IRecommendationDbContext dbContext;

        public GamesController(IRecommendationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Returns a paginated, searchable list of all games.
        /// </summary>
        /// <param name="search">Optional search term to filter games by name.</param>
        /// <param name="page">The one-based page number. Defaults to 1.</param>
        /// <param name="pageSize">The number of results per page. Defaults to 20.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<GameSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGames([FromQuery] string? search,[FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var query = dbContext.Games
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(game => game.Name.Contains(search));

            var totalCount = await query.CountAsync();

            var games = await query
                .OrderBy(game => game.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(game => new GameSummaryDto
                {
                    Id = game.Id,
                    Name = game.Name,
                    ImageUrl = game.ImageUrl,
                    ReleaseDate = game.ReleaseDate,
                    UserRating = game.UserRatings
                        .Where(rating => rating.UserId == userId)
                        .Select(rating => (RatingType?)rating.Rating)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new PagedResultDto<GameSummaryDto>
            {
                Items = games,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        /// <summary>
        /// Returns the full details for a single game.
        /// </summary>
        /// <param name="id">The game's unique identifier.</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(GameDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGame(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var game = await dbContext.Games
                .AsNoTracking()
                .Where(game => game.Id == id)
                .Select(game => new GameDetailDto
                {
                    Id = game.Id,
                    Name = game.Name,
                    Description = game.Description,
                    ImageUrl = game.ImageUrl,
                    ReleaseDate = game.ReleaseDate,
                    Tags = game.GameTags.Select(gameTag => gameTag.Tag.Name),
                    UserRating = game.UserRatings
                        .Where(rating => rating.UserId == userId)
                        .Select(rating => (RatingType?)rating.Rating)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (game == null)
                return NotFound();

            return Ok(game);
        }
    }
}
