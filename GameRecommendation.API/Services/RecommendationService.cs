using GameRecommendation.API.Algorithm;
using GameRecommendation.API.DataTransferObjects.Common;
using GameRecommendation.Domain.Models.Domain;
using GameRecommendation.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameRecommendation.API.Services
{

    /// <summary>
    /// Coordinates the recommendation pipeline for users.
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly IUserPreferenceBuilder userPreferenceBuilder;
        private readonly IRecommendationAlgorithm recommendationAlgorithm;
        private readonly IRecommendationDbContext dbContext;

        public RecommendationService(IRecommendationDbContext dbContext, IUserPreferenceBuilder userPreferenceBuilder, IRecommendationAlgorithm recommendationAlgorithm)
        {
            this.userPreferenceBuilder = userPreferenceBuilder;
            this.recommendationAlgorithm = recommendationAlgorithm;
            this.dbContext = dbContext;
        }

        /// <inheritdoc/>
        public async Task<PagedResult<RecommendationResult>> GetRecommendationsAsync(string userId, int page, int pageSize)
        {
            var ratings = await dbContext.UserRatings
                .Where(rating => rating.UserId == userId)
                .Include(rating => rating.Game)
                    .ThenInclude(game => game.GameTags)
                .AsNoTracking()
                .ToListAsync();

            var ratedGameIds = ratings.Select(rating => rating.GameId).ToHashSet();

            var candidateGames = await dbContext.Games
                .Where(game => !ratedGameIds.Contains(game.Id))
                .Include(game => game.GameTags)
                .AsNoTracking()
                .ToListAsync();

            var ratedGames = ratings.Select(rating => rating.Game).ToList();
            var preferenceProfile = userPreferenceBuilder.Build(ratings, ratedGames);

            var allResults = recommendationAlgorithm
                .GenerateRecommendations(preferenceProfile, candidateGames)
                .ToList();

            var items = allResults
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return new PagedResult<RecommendationResult>
            {
                Items = items,
                TotalCount = allResults.Count
            };
        }
    }
}
