using GameRecommendation.API.Algorithm;
using GameRecommendation.Domain.Models.Domain;

namespace GameRecommendation.API.Tests
{
    public class CosineSimilarityAlgorithmTests
    {
        private readonly CosineSimilarityAlgorithm algorithm = new();

        private static Game MakeGame(int id, params int[] tagIds) => new()
        {
            Id = id,
            Name = $"Game {id}",
            Description = $"Description for game {id}",
            ImageUrl = $"https://image/{id}.jpg",
            GameTags = tagIds.Select(t => new GameTag { GameId = id, TagId = t }).ToList()
        };

        private static UserPreferenceProfile MakeProfile(params (int tagId, double weight)[] weights) => new()
        {
            TagWeights = weights.ToDictionary(w => w.tagId, w => w.weight)
        };

        [Fact]
        [Trait("Category", "Unit")]
        public void Game_With_Matching_Tag_Scores_Higher_Than_Game_Without()
        {
            var profile = MakeProfile((10, 1.0));
            var goodGame = MakeGame(1, 10);
            var badGame = MakeGame(2, 99);

            var results = algorithm.GenerateRecommendations(profile, new[] { goodGame, badGame }).ToList();

            Assert.Equal(1, results.First().Game.Id);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Identical_Vectors_Return_Score_Of_One()
        {
            var profile = MakeProfile((10, 1.0));
            var game = MakeGame(1, 10);

            var result = algorithm.GenerateRecommendations(profile, new[] { game });

            Assert.Equal(1.0, result.First().Score);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void No_Shared_Tags_Returns_Score_Of_Zero()
        {
            var profile = MakeProfile((10, 1.0));
            var game = MakeGame(1, 99);

            var result = algorithm.GenerateRecommendations(profile, new[] { game });

            Assert.Equal(0.0, result.First().Score);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Results_Are_Ordered_By_Score_Descending()
        {
            var profile = MakeProfile((10, 1.0));
            var bestGame = MakeGame(1, 10);
            var worstGame = MakeGame(2, 99);
            var midGame = MakeGame(3, 10, 99);

            var results = algorithm.GenerateRecommendations(
                profile, new[] { worstGame, midGame, bestGame }).ToList();

            Assert.True(results[0].Score >= results[1].Score);
            Assert.True(results[1].Score >= results[2].Score);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Empty_Game_List_Returns_Empty_Results()
        {
            var profile = MakeProfile((10, 1.0));

            var results = algorithm.GenerateRecommendations(profile, Array.Empty<Game>());

            Assert.Empty(results);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Empty_User_Profile_Returns_Zero_For_All_Games()
        {
            var profile = MakeProfile();
            var game = MakeGame(1, 10);

            var result = algorithm.GenerateRecommendations(profile, new[] { game });

            Assert.Equal(0.0, result.First().Score);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Game_With_No_Tags_Returns_Score_Of_Zero()
        {
            var profile = MakeProfile((10, 1.0));
            var game = MakeGame(1);

            var result = algorithm.GenerateRecommendations(profile, new[] { game });

            Assert.Equal(0.0, result.First().Score);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Game_With_More_Matching_Tags_Scores_Higher()
        {
            var profile = MakeProfile((10, 1.0), (20, 1.0));
            var betterGame = MakeGame(1, 10, 20);
            var worseGame = MakeGame(2, 10);

            var results = algorithm.GenerateRecommendations(
                profile, new[] { worseGame, betterGame }).ToList();

            Assert.Equal(1, results.First().Game.Id);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Negative_User_Weight_Produces_Negative_Score()
        {
            var profile = MakeProfile((10, -1.0));
            var game = MakeGame(1, 10);

            var result = algorithm.GenerateRecommendations(profile, new[] { game });

            Assert.True(result.First().Score < 0);
        }
    }
}
