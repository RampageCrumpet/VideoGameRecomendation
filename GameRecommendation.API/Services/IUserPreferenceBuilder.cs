
using GameRecommendation.Domain.Models.Domain;

namespace GameRecommendation.API.Services
{
    /// <summary>
    /// A user preference builder is responsible for taking a user's historical ratings and the games they've rated and building a user preference profile that captures the user's preferences for different game tags.
    /// </summary>
    public interface IUserPreferenceBuilder
    {
        /// <summary>
        /// Builds a user preference profile based on the user's historical ratings and the games they've rated.
        /// </summary>
        /// <param name="ratings"> A collection of the users <see cref="UserRating"/>s used to build their preferences.</param>
        /// <param name="games"> A collection of <see cref="Game"/>s we rated.</param>
        /// <returns>Returns a <see cref="UserPreferenceProfile"/> </returns>
        UserPreferenceProfile Build(IEnumerable<UserRating> ratings, IEnumerable<Game> games);
    }
}
