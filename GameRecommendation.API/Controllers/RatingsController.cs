using GameRecommendation.API.DataTransferObjects.Ratings;
using GameRecommendation.Domain.Models.Domain;
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
    public class RatingsController : ControllerBase
    {
        private readonly IRecommendationDbContext dbContext;

        public RatingsController(IRecommendationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Creates or updates the calling user's rating for a game.
        /// </summary>
        /// <param name="request">The game ID and rating value.</param>
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpsertRating(UpsertRatingRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var gameExists = await dbContext.Games
                .AsNoTracking()
                .AnyAsync(game => game.Id == request.GameId);

            if (!gameExists)
                return NotFound();

            var existingRating = await dbContext.UserRatings
                .FirstOrDefaultAsync(rating =>
                    rating.UserId == userId &&
                    rating.GameId == request.GameId);

            if (existingRating == null)
            {
                dbContext.UserRatings.Add(new UserRating
                {
                    UserId = userId,
                    GameId = request.GameId,
                    Rating = request.Rating,
                    UpdatedUtc = DateTime.UtcNow
                });
            }
            else
            {
                existingRating.Rating = request.Rating;
                existingRating.UpdatedUtc = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Deletes the calling user's rating for a game.
        /// </summary>
        /// <param name="gameId">The ID of the game to remove the rating for.</param>
        [HttpDelete("{gameId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteRating(int gameId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var rating = await dbContext.UserRatings
                .FirstOrDefaultAsync(rating =>
                    rating.UserId == userId &&
                    rating.GameId == gameId);

            if (rating == null)
                return NotFound();

            dbContext.UserRatings.Remove(rating);
            await dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}
