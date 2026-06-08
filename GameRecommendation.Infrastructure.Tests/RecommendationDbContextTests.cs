using GameRecommendation.Domain.Models.Domain;
using GameRecommendation.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameRecommendation.Infrastructure.Tests
{
    public class RecommendationDbContextTests
    {
        private static RecommendationDbContext CreateContext() =>
            new RecommendationDbContext(
                new DbContextOptionsBuilder<RecommendationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

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
                () => context.SaveChangesAsync());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task SaveChanges_Throws_WhenRequiredTagNameMissing()
        {
            using var context = CreateContext();

            context.Tags.Add(new Tag { Name = null! });

            await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync());
        }
    }
}
