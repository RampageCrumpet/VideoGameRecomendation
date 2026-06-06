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
        [Trait("Category", "Unit")]
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

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_DoesNotDuplicate_WhenGameAlreadyExists()
        {
            var options = new DbContextOptionsBuilder<RecommendationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new RecommendationDbContext(options);

            db.Games.Add(new Game
            {
                SteamAppId = 123,
                Name = "Existing Game",
                Description = "I already exist",
                ImageUrl = "Some exising test image URL",
                ReleaseDate = DateTime.MinValue
            });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var fetcher = new Mock<ISteamGameFetcher>();
            var mapper = new Mock<ISteamGameMapper>();

            JsonDocument json = JsonDocument.Parse("""
            {
                "123": {
                    "success": true,
                    "data": {
                        "name": "Updated Game",
                        "short_description": "Updated description",
                        "header_image": "Updated image URL"
                    }
                }
            }
            """);

            fetcher.Setup(x => x.GetGameAsync(123))
                   .ReturnsAsync(json);

            mapper.Setup(x => x.Map(123, It.IsAny<JsonDocument>()))
                  .Returns(new Game
                  {
                      SteamAppId = 123,
                      Name = "Some new game",
                      Description = "some new test description",
                      ImageUrl = "Some new test image URL",
                      ReleaseDate = DateTime.UtcNow
                  });

            var runner = new SteamImportRunner(fetcher.Object, mapper.Object, db);

            await runner.ImportGamesAsync(new[] { 123 });

            Assert.Equal(1, db.Games.Count());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_UpdatesExistingGame_WhenGameAlreadyExists()
        {
            var options = new DbContextOptionsBuilder<RecommendationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new RecommendationDbContext(options);

            db.Games.Add(new Game
            {
                SteamAppId = 123,
                Name = "Existing Game",
                Description = "I already exist",
                ImageUrl = "Some exising test image URL",
                ReleaseDate = DateTime.MinValue
            });

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var fetcher = new Mock<ISteamGameFetcher>();
            var mapper = new Mock<ISteamGameMapper>();

            JsonDocument json = JsonDocument.Parse("""
            {
                "123": {
                    "success": true,
                    "data": {
                        "name": "Updated Game",
                        "short_description": "Updated description",
                        "header_image": "Updated image URL"
                    }
                }
            }
            """);

            fetcher.Setup(x => x.GetGameAsync(123))
                   .ReturnsAsync(json);

            mapper.Setup(x => x.Map(123, It.IsAny<JsonDocument>()))
                  .Returns(new Game
                  {
                      SteamAppId = 123,
                      Name = "Some new game",
                      Description = "some new test description",
                      ImageUrl = "Some new test image URL",
                      ReleaseDate = DateTime.UtcNow
                  });

            var runner = new SteamImportRunner(fetcher.Object, mapper.Object, db);

            await runner.ImportGamesAsync(new[] { 123 });

            Assert.Equal(1, db.Games.Count());

            var game = db.Games.Single();

            Assert.Equal(123, game.SteamAppId);
            Assert.Equal("Some new game", game.Name);
            Assert.Equal("some new test description", game.Description);
            Assert.Equal("Some new test image URL", game.ImageUrl);
            Assert.True(game.ReleaseDate > DateTime.MinValue);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_Skips_WhenMapperReturnsNull()
        {
            var options = new DbContextOptionsBuilder<RecommendationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new RecommendationDbContext(options);

            var fetcher = new Mock<ISteamGameFetcher>();
            var mapper = new Mock<ISteamGameMapper>();

            var json = JsonDocument.Parse("""
            {
                "123": {
                    "success": false
                }
            }
            """);

            fetcher.Setup(x => x.GetGameAsync(123))
                   .ReturnsAsync(json);

            mapper.Setup(x => x.Map(123, json))
                  .Returns((Game?)null);

            var runner = new SteamImportRunner(fetcher.Object, mapper.Object, db);

            await runner.ImportGamesAsync(new[] { 123 });

            Assert.Empty(db.Games);
        }
    }
}
