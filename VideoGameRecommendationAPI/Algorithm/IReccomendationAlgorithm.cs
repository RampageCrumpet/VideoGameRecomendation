
using GameRecommendation.Domain.Models.Domain;

namespace GameRecommendation.API.Algorithm
{
    /// <summary>
    /// A Recommendation algorithm takes a user's preferences and a list of candidate games and produces a list of games that best match the user's preferences.
    /// </summary>
    public interface IRecommendationAlgorithm
    {
        /// <summary>
        /// Generates a given number of game Recommendations based on the user preference profile and the list of games. 
        /// </summary>
        /// <param name="userPreferenceProfile"> The preferences of the user we're generating games for.</param>
        /// <param name="games"> The candidate games we're selecting from.</param>
        /// <returns>A collection of games that most closely match the users preferences.</returns>
        IEnumerable<RecommendationResult> GenerateRecommendations(UserPreferenceProfile userPreferenceProfile, IEnumerable<Game> games);
    }
}
