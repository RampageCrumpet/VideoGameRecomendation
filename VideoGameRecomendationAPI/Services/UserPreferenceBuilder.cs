using VideoGameRecomendationDomain.Models.Domain;
using VideoGameRecomendationDomain.Models.Enums;

namespace VideoGameRecomendationAPI.Services
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
                // If the game doesn't exist in our list of games skip it.
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
                    // If we can't find the tag in our dictionary, initialize it with a weight of 0. This sets it to no preference.
                    if (!tagWeights.ContainsKey(tag.TagId))
                    {
                        tagWeights[tag.TagId] = 0;
                    }

                    tagWeights[tag.TagId] += impact;
                }
            }

            return new UserPreferenceProfile
            {
                TagWeights = tagWeights
            };
        }
    }
}
