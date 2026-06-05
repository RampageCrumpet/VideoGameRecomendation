using GameRecomendation.Domain.Models.Domain;

namespace GameRecomendation.API.Services
{
    /// <summary>
    /// Generates game recommendations for users.
    /// </summary>
    public interface IRecommendationService
    {
        /// <summary>
        /// Generates a collection of game recommendations for a user.
        /// </summary>
        /// <param name="userId">The user to generate recommendations for.</param>
        /// <param name="numberOfRecommendations">The maximum number of recommendations to return.</param>
        /// <returns>A collection of recommendations ordered from best to worst match.</returns>
        IEnumerable<RecommendationResult> GetRecommendations(
            int userId,
            int numberOfRecommendations);
    }
}
