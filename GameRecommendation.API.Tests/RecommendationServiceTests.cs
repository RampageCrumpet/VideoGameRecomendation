using GameRecommendation.API.Algorithm;
using GameRecommendation.API.Services;
using GameRecommendation.Domain.Enums;
using GameRecommendation.Domain.Models.Domain;
using GameRecommendation.Infrastructure.Data;
using GameRecommendation.TestUtilities;
using Microsoft.EntityFrameworkCore;

namespace GameRecommendation.API.Tests
{
    public class RecommendationServiceTests
    {
        private readonly TestRecommendationDbContext dbContext;
        private readonly RecommendationService service;

        public RecommendationServiceTests()
        {
            var options = new DbContextOptionsBuilder<RecommendationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var testOptions = new DbContextOptionsBuilder<TestRecommendationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            dbContext = new TestRecommendationDbContext(testOptions);

            service = new RecommendationService(
                dbContext,
                new UserPreferenceBuilder(),
                new CosineSimilarityAlgorithm());
        }

        private static Game MakeGame(int id, string name, params int[] tagIds) => new()
        {
            Id = id,
            SteamAppId = id,
            Name = name,
            Description = $"Description for {name}",
            ImageUrl = $"https://image/{id}.jpg",
            GameTags = tagIds.Select(t => new GameTag { GameId = id, TagId = t, Weight = 1.0 }).ToList()
        };

        private static Tag MakeTag(int id, string name) => new() { Id = id, Name = name };

        private static User MakeUser(string id) => new() { Id = id, UserName = $"user_{id}" };

        private static UserRating MakeRating(string userId, int gameId, RatingType rating) => new()
        {
            UserId = userId,
            GameId = gameId,
            Rating = rating,
            UpdatedUtc = DateTime.UtcNow
        };

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendationsAsync_ReturnsOnlyUnratedGames()
        {
            var tag = MakeTag(1, "Action");
            var ratedGame = MakeGame(1, "Rated Game", 1);
            var unratedGame = MakeGame(2, "Unrated Game", 1);
            var user = MakeUser("user1");

            dbContext.Tags.Add(tag);
            dbContext.Games.AddRange(ratedGame, unratedGame);
            dbContext.RatingUsers.Add(user);
            dbContext.UserRatings.Add(MakeRating("user1", 1, RatingType.Like));
            await dbContext.SaveChangesAsync();

            var result = await service.GetRecommendationsAsync("user1", 1, 20);

            Assert.Single(result.Items);
            Assert.Equal(2, result.Items.First().Game.Id);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendationsAsync_ReturnsEmpty_WhenAllGamesAreRated()
        {
            var tag = MakeTag(1, "Action");
            var game = MakeGame(1, "Rated Game", 1);
            var user = MakeUser("user1");

            dbContext.Tags.Add(tag);
            dbContext.Games.Add(game);
            dbContext.RatingUsers.Add(user);
            dbContext.UserRatings.Add(MakeRating("user1", 1, RatingType.Like));
            await dbContext.SaveChangesAsync();

            var result = await service.GetRecommendationsAsync("user1", 1, 20);

            Assert.Empty(result.Items);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendationsAsync_ReturnsAllUnratedGames_WhenUserHasNoRatings()
        {
            var tag = MakeTag(1, "Action");
            dbContext.Tags.Add(tag);
            dbContext.Games.AddRange(MakeGame(1, "Game 1", 1), MakeGame(2, "Game 2", 1));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            await dbContext.SaveChangesAsync();

            var result = await service.GetRecommendationsAsync("user1", 1, 20);

            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendationsAsync_OrdersByScoreDescending()
        {
            var actionTag = MakeTag(1, "Action");
            var rpgTag = MakeTag(2, "RPG");
            var actionGame = MakeGame(1, "Action Game", 1);
            var rpgGame = MakeGame(2, "RPG Game", 2);
            var actionAndRpgGame = MakeGame(3, "Action RPG", 1, 2);
            var user = MakeUser("user1");

            dbContext.Tags.AddRange(actionTag, rpgTag);
            dbContext.Games.AddRange(actionGame, rpgGame, actionAndRpgGame);
            dbContext.RatingUsers.Add(user);
            dbContext.UserRatings.Add(MakeRating("user1", 1, RatingType.Like));
            await dbContext.SaveChangesAsync();

            var result = await service.GetRecommendationsAsync("user1", 1, 20);

            var scores = result.Items.Select(r => r.Score).ToList();
            Assert.True(scores[0] >= scores[1]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendationsAsync_RespectsPageSize()
        {
            var tag = MakeTag(1, "Action");
            dbContext.Tags.Add(tag);
            dbContext.Games.AddRange(
                MakeGame(1, "Game 1", 1),
                MakeGame(2, "Game 2", 1),
                MakeGame(3, "Game 3", 1));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            await dbContext.SaveChangesAsync();

            var result = await service.GetRecommendationsAsync("user1", 1, 2);

            Assert.Equal(2, result.Items.Count());
            Assert.Equal(3, result.TotalCount);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendationsAsync_ReturnsCorrectPage()
        {
            var tag = MakeTag(1, "Action");
            dbContext.Tags.Add(tag);
            dbContext.Games.AddRange(
                MakeGame(1, "Game 1", 1),
                MakeGame(2, "Game 2", 1),
                MakeGame(3, "Game 3", 1));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            await dbContext.SaveChangesAsync();

            var page1 = await service.GetRecommendationsAsync("user1", 1, 2);
            var page2 = await service.GetRecommendationsAsync("user1", 2, 2);

            Assert.Equal(2, page1.Items.Count());
            Assert.Single(page2.Items);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendationsAsync_DoesNotReturnGamesFromOtherUsersRatings()
        {
            var tag = MakeTag(1, "Action");
            var game1 = MakeGame(1, "Game 1", 1);
            var game2 = MakeGame(2, "Game 2", 1);

            dbContext.Tags.Add(tag);
            dbContext.Games.AddRange(game1, game2);
            dbContext.RatingUsers.AddRange(MakeUser("user1"), MakeUser("user2"));
            dbContext.UserRatings.Add(MakeRating("user2", 1, RatingType.Like));
            await dbContext.SaveChangesAsync();

            var result = await service.GetRecommendationsAsync("user1", 1, 20);

            Assert.Equal(2, result.TotalCount);
        }
    }
}
