using GameRecommendation.API.Controllers;
using GameRecommendation.API.DataTransferObjects.Ratings;
using GameRecommendation.Domain.Enums;
using GameRecommendation.Domain.Models.Domain;
using GameRecommendation.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameRecommendation.API.Tests
{
    public class RatingsControllerTests
    {
        private readonly TestRecommendationDbContext dbContext;
        private readonly RatingsController controller;

        public RatingsControllerTests()
        {
            var options = new DbContextOptionsBuilder<TestRecommendationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            dbContext = new TestRecommendationDbContext(options);
            controller = new RatingsController(dbContext);

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user1") }, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        private static Game MakeGame(int id, string name = "Game") => new()
        {
            Id = id,
            SteamAppId = id,
            Name = name,
            Description = "Description",
            ImageUrl = "https://image.jpg",
            ReleaseDate = new DateTime(2023, 1, 1)
        };

        private static User MakeUser(string id) => new() { Id = id, UserName = $"user_{id}" };

        // ── UpsertRating ──────────────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UpsertRating_CreatesNewRating_WhenNoneExists()
        {
            dbContext.Games.Add(MakeGame(1));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.UpsertRating(new UpsertRatingRequestDto { GameId = 1, Rating = RatingType.Like });

            Assert.IsType<NoContentResult>(result);
            var rating = Assert.Single(dbContext.UserRatings);
            Assert.Equal(RatingType.Like, rating.Rating);
            Assert.Equal("user1", rating.UserId);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UpsertRating_UpdatesExistingRating_WhenAlreadyExists()
        {
            dbContext.Games.Add(MakeGame(1));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            dbContext.UserRatings.Add(new UserRating { UserId = "user1", GameId = 1, Rating = RatingType.Like, UpdatedUtc = DateTime.UtcNow.AddDays(-1) });
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.UpsertRating(new UpsertRatingRequestDto { GameId = 1, Rating = RatingType.Dislike });

            Assert.IsType<NoContentResult>(result);
            var rating = Assert.Single(dbContext.UserRatings);
            Assert.Equal(RatingType.Dislike, rating.Rating);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UpsertRating_ReturnsNotFound_WhenGameDoesNotExist()
        {
            var result = await controller.UpsertRating(new UpsertRatingRequestDto { GameId = 999, Rating = RatingType.Like });

            Assert.IsType<NotFoundResult>(result);
            Assert.Empty(dbContext.UserRatings);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UpsertRating_ReturnsNotFound_WhenAuthenticatedUserHasNoRatingUsersRow()
        {
            // Simulates a valid JWT (NameIdentifier = "user1") for a user that was never
            // persisted to the RatingUsers table -- an inconsistent-account edge case.
            dbContext.Games.Add(MakeGame(1));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.UpsertRating(new UpsertRatingRequestDto { GameId = 1, Rating = RatingType.Like });

            Assert.IsType<NotFoundResult>(result);
            Assert.Empty(dbContext.UserRatings);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UpsertRating_DoesNotAffectOtherUsersRating()
        {
            dbContext.Games.Add(MakeGame(1));
            dbContext.RatingUsers.AddRange(MakeUser("user1"), MakeUser("user2"));
            dbContext.UserRatings.Add(new UserRating { UserId = "user2", GameId = 1, Rating = RatingType.Like, UpdatedUtc = DateTime.UtcNow });
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            await controller.UpsertRating(new UpsertRatingRequestDto { GameId = 1, Rating = RatingType.Dislike });

            Assert.Equal(2, dbContext.UserRatings.Count());
            Assert.Equal(RatingType.Like, dbContext.UserRatings.First(r => r.UserId == "user2").Rating);
        }

        // ── DeleteRating ──────────────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task DeleteRating_RemovesRating_WhenExists()
        {
            dbContext.Games.Add(MakeGame(1));
            dbContext.RatingUsers.Add(MakeUser("user1"));
            dbContext.UserRatings.Add(new UserRating { UserId = "user1", GameId = 1, Rating = RatingType.Like, UpdatedUtc = DateTime.UtcNow });
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.DeleteRating(1);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(dbContext.UserRatings);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task DeleteRating_ReturnsNotFound_WhenRatingDoesNotExist()
        {
            var result = await controller.DeleteRating(1);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task DeleteRating_DoesNotRemoveOtherUsersRating()
        {
            dbContext.Games.Add(MakeGame(1));
            dbContext.RatingUsers.AddRange(MakeUser("user1"), MakeUser("user2"));
            dbContext.UserRatings.Add(new UserRating { UserId = "user2", GameId = 1, Rating = RatingType.Like, UpdatedUtc = DateTime.UtcNow });
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await controller.DeleteRating(1);

            Assert.IsType<NotFoundResult>(result);
            Assert.Single(dbContext.UserRatings);
        }
    }
}
