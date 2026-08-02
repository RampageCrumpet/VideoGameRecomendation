
using GameRecommendation.Domain.Enums;
using GameRecommendation.Domain.Models.Domain;

namespace GameRecommendation.API.Services
{
    /// <summary>
    /// Builds a <see cref="UserPreferenceProfile"/> from the given ratings of the given games.
    /// </summary>
    public class UserPreferenceBuilder : IUserPreferenceBuilder
    {
        /// <inheritdoc/>
        public UserPreferenceProfile Build(IEnumerable<UserRating> ratings, IEnumerable<Game> games)
        {
            var tagWeights = new Dictionary<int, double>();

            var gameLookup = games.ToDictionary(g => g.Id);

            foreach (var rating in ratings)
            {
                if(!gameLookup.TryGetValue(rating.GameId, out var game))
                {
                    continue;
                }

                double impact = rating.Rating switch
                {
                    RatingType.Like => 1.0,
                    RatingType.Dislike => -1.0,
                    _ => 0.0
                };

                foreach (var tag in game.GameTags)
                {
                    // Use GetValueOrDefault so missing keys default to 0 and we can add the impact directly.
                    tagWeights[tag.TagId] = tagWeights.GetValueOrDefault(tag.TagId) + impact;
                }
            }

            return new UserPreferenceProfile
            {
                TagWeights = tagWeights
            };
        }
    }
}
