using GameRecomendation.Domain.Models.Domain;
using GameRecomendation.Infrastructure;
using GameRecomendation.SteamImporter.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;

namespace GameRecomendation.Tests.SteamImporterTests
{
    public class SteamImportRunnerTests
    {
        [Fact]
        public async Task ImportGamesAsync_AddsNewGame_WhenGameDoesNotExist()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<RecommendationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new RecommendationDbContext(options);

            var fetcher = new Mock<ISteamGameFetcher>();
            var mapper = new Mock<ISteamGameMapper>();

            var json = JsonDocument.Parse("""
            {
                "123": {
                    "success": true,
                    "data": {
                        "name": "Test Game",
                        "short_description": "desc",
                        "header_image": "img"
                    }
                }
            }
            """);

            fetcher.Setup(x => x.GetGameAsync(123))
                   .ReturnsAsync(json);

            mapper.Setup(x => x.Map(123, json))
                  .Returns(new Game
                  {
                      SteamAppId = 123,
                      Name = "Test Game",
                      Description = "Test description",
                      ImageUrl = "Some test image URL",
                      ReleaseDate = DateTime.UtcNow
                  });

            var runner = new SteamImportRunner(fetcher.Object, mapper.Object, db);

            // Act
            await runner.ImportGamesAsync(new[] { 123 });

            // Assert
            Assert.Equal(1, db.Games.Count());
        }
    }
}
