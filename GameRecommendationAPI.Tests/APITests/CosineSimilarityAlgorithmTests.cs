using GameRecommendation.API.Algorithm;
using GameRecommendation.API.Services;
using GameRecommendation.Domain.Enums;
using GameRecommendation.Domain.Models.Domain;

namespace GameRecommendation.API.Tests
{
    public class CosineSimilarityAlgorithmTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public void Game_With_Matching_Tag_Scores_Higher()
        {
            var userProfile = new UserPreferenceProfile
            {
                TagWeights = new Dictionary<int, double>
                {
                    { 10, 1.0 }
                }
            };

            var goodGame = new Game
            {
                Name = "first game",
                Description = "I am the first game.",
                ImageUrl = "first game test Url",
                Id = 1,
                GameTags = new List<GameTag>
                {
                    new GameTag { GameId = 1, TagId = 10 }
                }
            };

            var badGame = new Game
            {
                Name = "secondGame",
                Description = "I am the second game.",
                ImageUrl = "second game test Url",
                Id = 2,
                GameTags = new List<GameTag>
                {
                    new GameTag { GameId = 2, TagId = 99 }
                }
            };

            var algorithm = new CosineSimilarityAlgorithm();

            var results = algorithm.GenerateRecommendations(
                userProfile,
                new[] { goodGame, badGame }).ToList();

            Assert.Equal(1, results.First().Game.Id);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Identical_Vectors_Return_Maximum_Score()
        {
            var profile = new UserPreferenceProfile
            {
                TagWeights = new Dictionary<int, double>
                {
                    { 10, 1.0 }
                }
            };

            var game = new Game
            {
                Name = "testName",
                Description = "testDescription",
                ImageUrl = "testStringUrl",
                Id = 1,
                GameTags = new List<GameTag>
                {
                    new GameTag { GameId = 1, TagId = 10 }
                }
            };

            var algorithm = new CosineSimilarityAlgorithm();

            var result = algorithm.GenerateRecommendations(profile, new[] { game });

            Assert.Equal(1.0, result.First().Score);
        }

        [Fact]
        [Trait("Category","Unit")]
        public void No_Shared_Tags_Returns_Zero()
        {
            var profile = new UserPreferenceProfile
            {
                TagWeights = new Dictionary<int, double>
                {
                    { 10, 1.0 }
                }
            };

            var game = new Game
            {
                Name = "testName",
                Description = "testDescription",
                ImageUrl = "testStringUrl",
                Id = 1,
                GameTags = new List<GameTag>
                {
                    new GameTag { GameId = 1, TagId = 99 }
                }
            };

            var algorithm = new CosineSimilarityAlgorithm();

            var result = algorithm.GenerateRecommendations(profile, new[] { game });

            Assert.Equal(0, result.First().Score);
        }
    }
}
