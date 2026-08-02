using GameRecommendation.API.Services;
using GameRecommendation.Domain.Enums;
using GameRecommendation.Domain.Models.Domain;

namespace GameRecommendation.API.Tests
{
    public class UserPreferenceProfileBuilderTests
    {
        private readonly UserPreferenceBuilder builder = new();

        private static Game MakeGame(int id, params int[] tagIds) => new()
        {
            Id = id,
            Name = $"Game {id}",
            Description = $"Description for game {id}",
            ImageUrl = $"https://image/{id}.jpg",
            GameTags = tagIds.Select(t => new GameTag { GameId = id, TagId = t }).ToList()
        };

        private static UserRating MakeRating(int gameId, RatingType rating) => new()
        {
            GameId = gameId,
            Rating = rating
        };

        [Fact]
        [Trait("Category", "Unit")]
        public void Liked_Game_Increases_Tag_Weight()
        {
            var game = MakeGame(1, 10);
            var ratings = new[] { MakeRating(1, RatingType.Like) };

            var profile = builder.Build(ratings, new[] { game });

            Assert.Equal(1.0, profile.TagWeights[10]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Disliked_Game_Decreases_Tag_Weight()
        {
            var game = MakeGame(1, 10);
            var ratings = new[] { MakeRating(1, RatingType.Dislike) };

            var profile = builder.Build(ratings, new[] { game });

            Assert.Equal(-1.0, profile.TagWeights[10]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Skipped_Game_Does_Not_Change_Tag_Weight()
        {
            var game = MakeGame(1, 10);
            var ratings = new[] { MakeRating(1, RatingType.Skip) };

            var profile = builder.Build(ratings, new[] { game });

            Assert.Equal(0.0, profile.TagWeights[10]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Multiple_Likes_Accumulate_Tag_Weight()
        {
            var game1 = MakeGame(1, 10);
            var game2 = MakeGame(2, 10);
            var ratings = new[] { MakeRating(1, RatingType.Like), MakeRating(2, RatingType.Like) };

            var profile = builder.Build(ratings, new[] { game1, game2 });

            Assert.Equal(2.0, profile.TagWeights[10]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Like_And_Dislike_On_Same_Tag_Cancel_Out()
        {
            var game1 = MakeGame(1, 10);
            var game2 = MakeGame(2, 10);
            var ratings = new[] { MakeRating(1, RatingType.Like), MakeRating(2, RatingType.Dislike) };

            var profile = builder.Build(ratings, new[] { game1, game2 });

            Assert.Equal(0.0, profile.TagWeights[10]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Game_With_Multiple_Tags_Updates_All_Tags()
        {
            var game = MakeGame(1, 10, 20);
            var ratings = new[] { MakeRating(1, RatingType.Like) };

            var profile = builder.Build(ratings, new[] { game });

            Assert.Equal(1.0, profile.TagWeights[10]);
            Assert.Equal(1.0, profile.TagWeights[20]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Rating_For_Unknown_Game_Is_Ignored()
        {
            var game = MakeGame(1, 10);
            var ratings = new[] { MakeRating(999, RatingType.Like) };

            var profile = builder.Build(ratings, new[] { game });

            Assert.Empty(profile.TagWeights);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Empty_Ratings_Returns_Empty_Profile()
        {
            var game = MakeGame(1, 10);

            var profile = builder.Build(Array.Empty<UserRating>(), new[] { game });

            Assert.Empty(profile.TagWeights);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Empty_Games_Returns_Empty_Profile()
        {
            var ratings = new[] { MakeRating(1, RatingType.Like) };

            var profile = builder.Build(ratings, Array.Empty<Game>());

            Assert.Empty(profile.TagWeights);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Different_Tags_On_Different_Games_Are_Tracked_Independently()
        {
            var game1 = MakeGame(1, 10);
            var game2 = MakeGame(2, 20);
            var ratings = new[]
            {
                MakeRating(1, RatingType.Like),
                MakeRating(2, RatingType.Dislike)
            };

            var profile = builder.Build(ratings, new[] { game1, game2 });

            Assert.Equal(1.0, profile.TagWeights[10]);
            Assert.Equal(-1.0, profile.TagWeights[20]);
        }
    }
}
