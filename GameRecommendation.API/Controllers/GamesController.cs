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
        /// Returns a paginated, searchable list of games.
        /// By default this endpoint returns only games the current user has rated. If a search term is provided,
        /// results will include unrated games that match the search.
        /// </summary>
        /// <param name="search">Optional search term to filter games by name.</param>
        /// <param name="page">The one-based page number. Defaults to 1.</param>
        /// <param name="pageSize">The number of results per page. Defaults to 20.</param>
        /// <param name="ratedOnly">When true (default) limits results to games the user has rated when not searching.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<GameSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGames([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool ratedOnly = true)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest("Page and pageSize must be greater than zero.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var query = dbContext.Games
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(game =>
                    game.Name.Contains(s) ||
                    game.Description.Contains(s) ||
                    game.GameTags.Any(gt => gt.Tag.Name.Contains(s))
                );
            }

            // If ratedOnly is requested and there is no search term, filter to only games the user has rated.
            if (ratedOnly && string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(game => game.UserRatings.Any(rating => rating.UserId == userId));
            }

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

        /// <summary>
        /// Returns a single unrated game for the current user (randomized).
        /// </summary>
        [HttpGet("unrated")]
        [ProducesResponseType(typeof(GameDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetUnrated()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var game = await dbContext.Games
                .AsNoTracking()
                .Where(g => !g.UserRatings.Any(r => r.UserId == userId))
                .OrderBy(g => Guid.NewGuid())
                .Select(g => new GameDetailDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    ImageUrl = g.ImageUrl,
                    ReleaseDate = g.ReleaseDate,
                    Tags = g.GameTags.Select(gt => gt.Tag.Name),
                    UserRating = null
                })
                .FirstOrDefaultAsync();

            if (game == null)
                return NoContent();

            return Ok(game);
        }
    }
}
