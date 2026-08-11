using GameRecommendation.API.Controllers;
using GameRecommendation.API.DataTransferObjects.Common;
using GameRecommendation.API.DataTransferObjects.Games;
using GameRecommendation.Domain.Enums;
using GameRecommendation.Domain.Models.Domain;
using GameRecommendation.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameRecommendation.API.Tests
{
    public class GamesControllerTests
    {
        private readonly TestRecommendationDbContext dbContext;
        private readonly GamesController controller;

        public GamesControllerTests()
        {
            var options = new DbContextOptionsBuilder<TestRecommendationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            dbContext = new TestRecommendationDbContext(options);
            controller = new GamesController(dbContext);
            SetUser("user1");
        }

        private void SetUser(string userId)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        private static Game MakeGame(int id, string name, params int[] tagIds) => new()
        {
            Id = id,
            SteamAppId = id,
            Name = name,
            Description = $"Description for {name}",
            ImageUrl = $"https://image/{id}.jpg",
            ReleaseDate = new DateTime(2023, 1, 1),
            GameTags = tagIds.Select(t => new GameTag { GameId = id, TagId = t }).ToList()
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
        public async Task GetGames_ReturnsOnlyRatedGames_ByDefault()
        {
            dbContext.Games.AddRange(MakeGame(1, "Rated Game"), MakeGame(2, "Unrated Game"));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            dbContext.UserRatings.Add(MakeRating("user1", 1, RatingType.Like));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.GetGames(null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<PagedResultDto<GameSummaryDto>>(ok.Value);
            Assert.Single(payload.Items);
            Assert.Equal(1, payload.Items.First().Id);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGames_ReturnsUnratedMatches_WhenSearching()
        {
            dbContext.Games.AddRange(MakeGame(1, "Counter-Strike"), MakeGame(2, "Half-Life"));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.GetGames("Counter");

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<PagedResultDto<GameSummaryDto>>(ok.Value);
            Assert.Single(payload.Items);
            Assert.Equal("Counter-Strike", payload.Items.First().Name);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGames_TreatsWhitespaceOnlySearch_AsNoSearch()
        {
            dbContext.Games.AddRange(MakeGame(1, "Rated Game"), MakeGame(2, "Unrated Game"));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            dbContext.UserRatings.Add(MakeRating("user1", 1, RatingType.Like));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            // A whitespace-only search should fall through IsNullOrWhiteSpace and behave exactly
            // like no search at all -- including the ratedOnly default filter still applying.
            var result = await controller.GetGames("   ");

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<PagedResultDto<GameSummaryDto>>(ok.Value);
            Assert.Single(payload.Items);
            Assert.Equal(1, payload.Items.First().Id);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGames_SearchMatchesTagName()
        {
            dbContext.Tags.Add(MakeTag(1, "Roguelike"));
            dbContext.Games.Add(MakeGame(1, "Some Game", 1));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.GetGames("Roguelike");

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<PagedResultDto<GameSummaryDto>>(ok.Value);
            Assert.Single(payload.Items);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGames_ReturnsAllGames_WhenRatedOnlyIsFalseAndNotSearching()
        {
            dbContext.Games.AddRange(MakeGame(1, "Game 1"), MakeGame(2, "Game 2"));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.GetGames(null, ratedOnly: false);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<PagedResultDto<GameSummaryDto>>(ok.Value);
            Assert.Equal(2, payload.TotalCount);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGames_RespectsPageSize()
        {
            dbContext.Games.AddRange(MakeGame(1, "Game 1"), MakeGame(2, "Game 2"), MakeGame(3, "Game 3"));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.GetGames(null, page: 1, pageSize: 2, ratedOnly: false);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<PagedResultDto<GameSummaryDto>>(ok.Value);
            Assert.Equal(2, payload.Items.Count());
            Assert.Equal(3, payload.TotalCount);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGames_ReturnsCorrectPage()
        {
            dbContext.Games.AddRange(MakeGame(1, "Game 1"), MakeGame(2, "Game 2"), MakeGame(3, "Game 3"));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.GetGames(null, page: 2, pageSize: 2, ratedOnly: false);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<PagedResultDto<GameSummaryDto>>(ok.Value);
            Assert.Single(payload.Items);
            Assert.Equal(2, payload.Page);
            // Results are ordered by Name; page 1 of size 2 holds "Game 1"/"Game 2", so page 2
            // must hold "Game 3". Without this, the test would pass even if pagination silently
            // returned page 1's data again.
            Assert.Equal("Game 3", payload.Items.First().Name);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGames_IncludesCallingUsersRating()
        {
            dbContext.Games.Add(MakeGame(1, "Game 1"));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            dbContext.UserRatings.Add(MakeRating("user1", 1, RatingType.Dislike));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.GetGames(null, ratedOnly: true);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<PagedResultDto<GameSummaryDto>>(ok.Value);
            Assert.Equal(RatingType.Dislike, payload.Items.First().UserRating);
        }

        [Theory]
        [Trait("Category", "Unit")]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetGames_ReturnsBadRequest_WhenPageIsLessThanOne(int page)
        {
            var result = await controller.GetGames(null, page: page);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Theory]
        [Trait("Category", "Unit")]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetGames_ReturnsBadRequest_WhenPageSizeIsLessThanOne(int pageSize)
        {
            var result = await controller.GetGames(null, pageSize: pageSize);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGames_ReturnsEmpty_WhenNoGamesMatch()
        {
            dbContext.RatingUsers.Add(MakeUser("user1"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.GetGames(null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<PagedResultDto<GameSummaryDto>>(ok.Value);
            Assert.Empty(payload.Items);
            Assert.Equal(0, payload.TotalCount);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGame_ReturnsGameDetail_WhenFound()
        {
            dbContext.Tags.Add(MakeTag(1, "Action"));
            dbContext.Games.Add(MakeGame(1, "Game 1", 1));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.GetGame(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<GameDetailDto>(ok.Value);
            Assert.Equal("Game 1", payload.Name);
            Assert.Contains("Action", payload.Tags);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGame_ReturnsNotFound_WhenGameDoesNotExist()
        {
            var result = await controller.GetGame(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGame_IncludesCallingUsersRating()
        {
            dbContext.Games.Add(MakeGame(1, "Game 1"));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            dbContext.UserRatings.Add(MakeRating("user1", 1, RatingType.Like));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.GetGame(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<GameDetailDto>(ok.Value);
            Assert.Equal(RatingType.Like, payload.UserRating);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGame_DoesNotIncludeOtherUsersRating()
        {
            dbContext.Games.Add(MakeGame(1, "Game 1"));
            dbContext.RatingUsers.AddRange(MakeUser("user1"), MakeUser("user2"));
            dbContext.UserRatings.Add(MakeRating("user2", 1, RatingType.Like));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.GetGame(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<GameDetailDto>(ok.Value);
            Assert.Null(payload.UserRating);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetUnrated_ReturnsGame_NotYetRatedByUser()
        {
            dbContext.Games.Add(MakeGame(1, "Game 1"));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.GetUnrated();

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<GameDetailDto>(ok.Value);
            Assert.Equal(1, payload.Id);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetUnrated_ReturnsNoContent_WhenAllGamesRated()
        {
            dbContext.Games.Add(MakeGame(1, "Game 1"));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            dbContext.UserRatings.Add(MakeRating("user1", 1, RatingType.Like));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.GetUnrated();

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetUnrated_ReturnsNoContent_WhenNoGamesExist()
        {
            var result = await controller.GetUnrated();

            Assert.IsType<NoContentResult>(result);
        }
    }
}
