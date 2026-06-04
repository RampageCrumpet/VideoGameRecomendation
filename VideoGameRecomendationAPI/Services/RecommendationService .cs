using VideoGameRecomendationAPI.Algorithm;
using VideoGameRecomendationDomain.Models.Domain;

namespace VideoGameRecomendationAPI.Services
{

    /// <summary>
    /// Coordinates the recommendation pipeline for users.
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly IUserPreferenceBuilder _userPreferenceBuilder;
        private readonly IRecommendationAlgorithm _recommendationAlgorithm;

        public RecommendationService(IUserPreferenceBuilder userPreferenceBuilder, IRecommendationAlgorithm recommendationAlgorithm)
        {
            _userPreferenceBuilder = userPreferenceBuilder;
            _recommendationAlgorithm = recommendationAlgorithm;
        }

        /// <summary>
        /// Generates a collection of game recommendations for a user.
        /// </summary>
        /// <param name="userId">The user to generate recommendations for.</param>
        /// <param name="numberOfRecommendations">The maximum number of recommendations to return.</param>
        /// <returns>A collection of recommendations ordered from best to worst match.</returns>
        public IEnumerable<RecommendationResult> GetRecommendations(int userId,int numberOfRecommendations)
        {
            throw new NotImplementedException();
        }
    }
}
