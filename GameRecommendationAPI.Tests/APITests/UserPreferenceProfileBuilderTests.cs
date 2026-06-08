using GameRecommendation.API.Services;
using GameRecommendation.Domain.Enums;
using GameRecommendation.Domain.Models.Domain;

namespace GameRecommendation.API.Tests
{
    public class UserPreferenceProfileBuilderTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public void Liked_Game_Increases_Tag_Weight()
        {
            // Arrange
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

            var ratings = new List<UserRating>
            {
                new UserRating
                {
                    GameId = 1,
                    Rating = RatingType.Like
                }
            };

            var builder = new UserPreferenceBuilder();

            // Act
            var profile = builder.Build(ratings, new[] { game });

            // Assert
            Assert.Equal(1.0, profile.TagWeights[10]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Disliked_Game_Decreases_Tag_Weight()
        {
            //Arrange
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

            var ratings = new List<UserRating>
            {
                new UserRating
                {
                    GameId = 1,
                    Rating = RatingType.Dislike
                }
            };

            var builder = new UserPreferenceBuilder();

            // Act
            var profile = builder.Build(ratings, new[] { game });

            // Assert
            Assert.Equal(-1.0, profile.TagWeights[10]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Multiple_Likes_Accumulate_Tag_Weight()
        {
            var game1 = new Game
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

            var game2 = new Game
            {
                Name = "secondGame",
                Description = "I am the second game.",
                ImageUrl = "second game test Url",
                Id = 2,
                GameTags = new List<GameTag>
                {
                    new GameTag { GameId = 2, TagId = 10 }
                }
            };

            var ratings = new List<UserRating>
            {
                new UserRating { GameId = 1, Rating = RatingType.Like },
                new UserRating { GameId = 2, Rating = RatingType.Like }
            };

            var builder = new UserPreferenceBuilder();

            var profile = builder.Build(ratings, new[] { game1, game2 });

            Assert.Equal(2.0, profile.TagWeights[10]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Game_With_Multiple_Tags_Updates_All_Tags()
        {
            //Arrange
            var game = new Game
            {
                Name = "testName",
                Description = "testDescription",
                ImageUrl = "testStringUrl",
                Id = 1,
                GameTags = new List<GameTag>
                {
                    new GameTag { GameId = 1, TagId = 10 },
                    new GameTag { GameId = 1, TagId = 20 }
                }
            };

            var ratings = new List<UserRating>
            {
                new UserRating
                {
                    GameId = 1,
                    Rating = RatingType.Like
                }
            };

            var builder = new UserPreferenceBuilder();

            //Act
            var profile = builder.Build(ratings, new[] { game });

            //Assert
            Assert.Equal(1.0, profile.TagWeights[10]);
            Assert.Equal(1.0, profile.TagWeights[20]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Liked_Game_Increases_Tag_Weights()
        {
            // Arrange
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

            var ratings = new List<UserRating>
            {
                new UserRating
                {
                    GameId = 1,
                    Rating = RatingType.Like
                }
            };

            var builder = new UserPreferenceBuilder();

            // Act
            var profile = builder.Build(ratings, new[] { game });

            // Assert
            Assert.Equal(1.0, profile.TagWeights[10]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Disliked_Game_Decreases_Tag_Weights()
        {
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

            var ratings = new List<UserRating>
            {
                new UserRating
                {
                    GameId = 1,
                    Rating = RatingType.Dislike
                }
            };

            var builder = new UserPreferenceBuilder();

            var profile = builder.Build(ratings, new[] { game });

            Assert.Equal(-1.0, profile.TagWeights[10]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Multiple_Likes_Accumulate_Tag_Weights()
        {
            var game1 = new Game
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

            var game2 = new Game
            {
                Name = "secondGame",
                Description = "I am the second game.",
                ImageUrl = "second game test Url",
                Id = 2,
                GameTags = new List<GameTag>
                {
                    new GameTag { GameId = 2, TagId = 10 }
                }
            };

            var ratings = new List<UserRating>
            {
                new UserRating { GameId = 1, Rating = RatingType.Like },
                new UserRating { GameId = 2, Rating = RatingType.Like }
            };

            var builder = new UserPreferenceBuilder();

            var profile = builder.Build(ratings, new[] { game1, game2 });

            Assert.Equal(2.0, profile.TagWeights[10]);
        }
    }
}
