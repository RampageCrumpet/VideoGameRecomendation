using VideoGameRecomendationDomain.Models.Domain;

namespace GameRecomendation.Application.Algorithm
{
    public class CosineSimilarityAlgorithm : IRecommendationAlgorithm
    {
        ///<inheritdoc/>
        public IEnumerable<RecommendationResult> GenerateRecommendations(UserPreferenceProfile userPreferenceProfile, IEnumerable<Game> games)
        {
            var userVector = userPreferenceProfile.TagWeights;

            var results = new List<RecommendationResult>();

            foreach (var game in games)
            {
                var gameVector = BuildGameVector(game);

                var score = CosineSimilarity(userVector, gameVector);

                results.Add(new RecommendationResult
                {
                    Game = game,
                    Score = score
                });
            }

            return results.OrderByDescending(x => x.Score);
        }

        /// <summary>
        /// Converts a game into a sparse feature vector where each tag is represented as a dimension.
        /// Each tag present on the game is assigned a weight of 1.0, indicating binary presence in the feature space.
        /// This vector is used as input for similarity-based recommendation scoring.
        /// <param name="game"> The <see cref="Game"/> we're building a feature vector for.</param>
        /// <returns> The <see cref="Game"/>'s feature vector for tags it has,.</returns>
        private Dictionary<int, double> BuildGameVector(Game game)
        {
            var vector = new Dictionary<int, double>();

            foreach (var tag in game.GameTags)
            {
                vector[tag.TagId] = 1.0;
            }

            return vector;
        }

        /// <summary>
        /// Computes the cosine similarity between two spare feature vectors.
        /// The result represents the aungular similarity between the user's preferences and the game's features, with a value of 1 indicating perfect similarity and 0 indicating no similarity.
        /// </summary>
        /// <param name="a"> The first feature vector we want to compare.</param>
        /// <param name="b"> The second feature vector we want to compare.</param>
        /// <returns> A scalar value showing the angular similarity between the two feature vectors.</returns>
        private double CosineSimilarity(Dictionary<int, double> a, Dictionary<int, double> b)
        {
            var intersection = a.Keys.Intersect(b.Keys);

            double dot = intersection.Sum(k => a[k] * b[k]);

            double magA = Math.Sqrt(a.Values.Sum(v => v * v));
            double magB = Math.Sqrt(b.Values.Sum(v => v * v));

            if (magA == 0 || magB == 0)
                return 0;

            return dot / (magA * magB);
        }
    }
}
