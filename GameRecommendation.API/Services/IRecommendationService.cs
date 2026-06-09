using GameRecommendation.API.DataTransferObjects.Common;
using GameRecommendation.Domain.Models.Domain;

namespace GameRecommendation.API.Services
{
    /// <summary>
    /// Generates game recommendations for users.
    /// </summary>
    public interface IRecommendationService
    {
        /// <summary>
        /// Generates a paginated collection of game recommendations for a user.
        /// </summary>
        /// <param name="userId">The ID of the user to generate recommendations for.</param>
        /// <param name="page">The one-based page number.</param>
        /// <param name="pageSize">The number of recommendations per page.</param>
        /// <returns>A paginated collection of recommendations ordered from best to worst match.</returns>
        Task<PagedResult<RecommendationResult>> GetRecommendationsAsync(string userId, int page, int pageSize);
    }
}
