using GameRecommendation.Domain.Enums;
using GameRecommendation.Domain.Models.Domain;
using GameRecommendation.TestUtilities;
using Microsoft.EntityFrameworkCore;

namespace GameRecommendation.Infrastructure.Tests
{
    public class RecommendationDbContextTests
    {
        private static TestRecommendationDbContext CreateContext() =>
            new TestRecommendationDbContext(
                new DbContextOptionsBuilder<TestRecommendationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

        private static Game MakeGame(int id, int steamAppId, string name = "Test Game") => new()
        {
            Id = id,
            SteamAppId = steamAppId,
            Name = name,
            Description = "Test description",
            ImageUrl = "https://image.jpg",
            ReleaseDate = DateTime.UtcNow
        };

        private static Tag MakeTag(int id, string name) => new() { Id = id, Name = name };

        private static User MakeUser(string id) => new() { Id = id, UserName = $"user_{id}" };

        // ── Game constraints ──────────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SaveChanges_Throws_WhenRequiredGameNameMissing()
        {
            using var context = CreateContext();
            context.Games.Add(new Game
            {
                SteamAppId = 730,
                Name = null!,
                Description = "A shooter game.",
                ImageUrl = "https://image.jpg",
                ReleaseDate = DateTime.MinValue
            });

            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SaveChanges_Throws_WhenRequiredGameDescriptionMissing()
        {
            using var context = CreateContext();
            context.Games.Add(new Game
            {
                SteamAppId = 730,
                Name = "Test Game",
                Description = null!,
                ImageUrl = "https://image.jpg",
                ReleaseDate = DateTime.MinValue
            });

            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SaveChanges_Throws_WhenRequiredGameImageUrlMissing()
        {
            using var context = CreateContext();
            context.Games.Add(new Game
            {
                SteamAppId = 730,
                Name = "Test Game",
                Description = "Test description",
                ImageUrl = null!,
                ReleaseDate = DateTime.MinValue
            });

            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SaveChanges_PersistsGame_WhenAllRequiredFieldsProvided()
        {
            using var context = CreateContext();
            context.Games.Add(MakeGame(1, 730));

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Assert.Single(context.Games);
        }

        // ── Tag constraints ───────────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SaveChanges_Throws_WhenRequiredTagNameMissing()
        {
            using var context = CreateContext();
            context.Tags.Add(new Tag { Name = null! });

            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SaveChanges_PersistsTag_WhenNameProvided()
        {
            using var context = CreateContext();
            context.Tags.Add(MakeTag(1, "Action"));

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Assert.Single(context.Tags);
        }

        // ── User constraints ──────────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SaveChanges_PersistsUser_WhenAllRequiredFieldsProvided()
        {
            using var context = CreateContext();
            context.RatingUsers.Add(MakeUser("user1"));

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Assert.Single(context.RatingUsers);
        }

        // ── GameTag relationships ─────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SaveChanges_PersistsGameTag_WhenGameAndTagExist()
        {
            using var context = CreateContext();
            var game = MakeGame(1, 730);
            var tag = MakeTag(1, "Action");

            context.Games.Add(game);
            context.Tags.Add(tag);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            context.GameTags.Add(new GameTag { GameId = 1, TagId = 1, Weight = 1.0 });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Assert.Single(context.GameTags);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GameTag_HasCompositeKey_OnGameIdAndTagId()
        {
            using var context = CreateContext();
            var game = MakeGame(1, 730);
            var tag = MakeTag(1, "Action");

            context.Games.Add(game);
            context.Tags.Add(tag);
            context.GameTags.Add(new GameTag { GameId = 1, TagId = 1, Weight = 1.0 });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Assert.Throws<InvalidOperationException>(() =>
                context.GameTags.Add(new GameTag { GameId = 1, TagId = 1, Weight = 2.0 }));
        }

        // ── UserRating relationships ──────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SaveChanges_PersistsUserRating_WhenUserAndGameExist()
        {
            using var context = CreateContext();
            var game = MakeGame(1, 730);
            var user = MakeUser("user1");

            context.Games.Add(game);
            context.RatingUsers.Add(user);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            context.UserRatings.Add(new UserRating
            {
                UserId = "user1",
                GameId = 1,
                Rating = RatingType.Like,
                UpdatedUtc = DateTime.UtcNow
            });

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Assert.Single(context.UserRatings);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UserRating_HasCompositeKey_OnUserIdAndGameId()
        {
            using var context = CreateContext();
            var game = MakeGame(1, 730);
            var user = MakeUser("user1");

            context.Games.Add(game);
            context.RatingUsers.Add(user);
            context.UserRatings.Add(new UserRating
            {
                UserId = "user1",
                GameId = 1,
                Rating = RatingType.Like,
                UpdatedUtc = DateTime.UtcNow
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Assert.Throws<InvalidOperationException>(() =>
                context.UserRatings.Add(new UserRating
                {
                    UserId = "user1",
                    GameId = 1,
                    Rating = RatingType.Dislike,
                    UpdatedUtc = DateTime.UtcNow
                }));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UserRatings_AreLoadedWithGame_WhenIncluded()
        {
            using var context = CreateContext();
            var game = MakeGame(1, 730);
            var user = MakeUser("user1");

            context.Games.Add(game);
            context.RatingUsers.Add(user);
            context.UserRatings.Add(new UserRating
            {
                UserId = "user1",
                GameId = 1,
                Rating = RatingType.Like,
                UpdatedUtc = DateTime.UtcNow
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var loadedGame = await context.Games
                .Include(g => g.UserRatings)
                .FirstAsync();

            Assert.Single(loadedGame.UserRatings);
            Assert.Equal(RatingType.Like, loadedGame.UserRatings.First().Rating);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GameTags_AreLoadedWithGame_WhenIncluded()
        {
            using var context = CreateContext();
            var game = MakeGame(1, 730);
            var tag = MakeTag(1, "Action");

            context.Games.Add(game);
            context.Tags.Add(tag);
            context.GameTags.Add(new GameTag { GameId = 1, TagId = 1, Weight = 1.0 });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var loadedGame = await context.Games
                .Include(g => g.GameTags)
                .ThenInclude(gt => gt.Tag)
                .FirstAsync();

            Assert.Single(loadedGame.GameTags);
            Assert.Equal("Action", loadedGame.GameTags.First().Tag.Name);
        }
    }
}
