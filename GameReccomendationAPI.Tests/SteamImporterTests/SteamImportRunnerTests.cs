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
            var testHarness = new SteamImportTestHarness();

            JsonDocument steamResponse = JsonDocument.Parse("""
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

            testHarness.Fetcher
                .Setup(fetcher => fetcher.GetGameAsync(123))
                .ReturnsAsync(steamResponse);

            testHarness.Mapper
                .Setup(mapper => mapper.Map(123, steamResponse))
                .Returns(new Game
                {
                    SteamAppId = 123,
                    Name = "Test Game",
                    Description = "Test description",
                    ImageUrl = "Some test image URL",
                    ReleaseDate = DateTime.UtcNow
                });

            testHarness.TagExtractor
                .Setup(extractor => extractor.Extract(It.IsAny<JsonElement>()))
                .Returns(new List<string>());

            await testHarness.Runner.ImportGamesAsync(new[] { 123 });

            Assert.Single(testHarness.Database.Games);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_DoesNotDuplicate_WhenGameAlreadyExists()
        {
            var testHarness = new SteamImportTestHarness();

            testHarness.Database.Games.Add(new Game
            {
                SteamAppId = 123,
                Name = "Existing Game",
                Description = "I already exist",
                ImageUrl = "Some existing test image URL",
                ReleaseDate = DateTime.MinValue
            });

            await testHarness.Database.SaveChangesAsync(TestContext.Current.CancellationToken);

            JsonDocument steamResponse = JsonDocument.Parse("""
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

            testHarness.Fetcher
                .Setup(fetcher => fetcher.GetGameAsync(123))
                .ReturnsAsync(steamResponse);

            testHarness.Mapper
                .Setup(mapper => mapper.Map(123, It.IsAny<JsonDocument>()))
                .Returns(new Game
                {
                    SteamAppId = 123,
                    Name = "Some new game",
                    Description = "some new test description",
                    ImageUrl = "Some new test image URL",
                    ReleaseDate = DateTime.UtcNow
                });

            testHarness.TagExtractor
                .Setup(extractor => extractor.Extract(It.IsAny<JsonElement>()))
                .Returns(new List<string>());

            await testHarness.Runner.ImportGamesAsync(new[] { 123 });

            Assert.Single(testHarness.Database.Games);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_UpdatesExistingGame_WhenGameAlreadyExists()
        {
            var testHarness = new SteamImportTestHarness();

            testHarness.Database.Games.Add(new Game
            {
                SteamAppId = 123,
                Name = "Existing Game",
                Description = "I already exist",
                ImageUrl = "Some existing test image URL",
                ReleaseDate = DateTime.MinValue
            });

            await testHarness.Database.SaveChangesAsync(TestContext.Current.CancellationToken);

            JsonDocument steamResponse = JsonDocument.Parse("""
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

            testHarness.Fetcher
                .Setup(fetcher => fetcher.GetGameAsync(123))
                .ReturnsAsync(steamResponse);

            testHarness.Mapper
                .Setup(mapper => mapper.Map(123, It.IsAny<JsonDocument>()))
                .Returns(new Game
                {
                    SteamAppId = 123,
                    Name = "Some new game",
                    Description = "some new test description",
                    ImageUrl = "Some new test image URL",
                    ReleaseDate = DateTime.UtcNow
                });

            testHarness.TagExtractor
                .Setup(extractor => extractor.Extract(It.IsAny<JsonElement>()))
                .Returns(new List<string>());

            await testHarness.Runner.ImportGamesAsync(new[] { 123 });

            Game updatedGame = testHarness.Database.Games.Single();

            Assert.Equal(123, updatedGame.SteamAppId);
            Assert.Equal("Some new game", updatedGame.Name);
            Assert.Equal("some new test description", updatedGame.Description);
            Assert.Equal("Some new test image URL", updatedGame.ImageUrl);
            Assert.True(updatedGame.ReleaseDate > DateTime.MinValue);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_Skips_WhenMapperReturnsNull()
        {
            var testHarness = new SteamImportTestHarness();

            JsonDocument steamResponse = JsonDocument.Parse("""
            {
                "123": {
                    "success": false
                }
            }
            """);

            testHarness.Fetcher
                .Setup(fetcher => fetcher.GetGameAsync(123))
                .ReturnsAsync(steamResponse);

            testHarness.Mapper
                .Setup(mapper => mapper.Map(123, steamResponse))
                .Returns((Game?)null);

            testHarness.TagExtractor
                .Setup(extractor => extractor.Extract(It.IsAny<JsonElement>()))
                .Returns(new List<string>());

            await testHarness.Runner.ImportGamesAsync(new[] { 123 });

            Assert.Empty(testHarness.Database.Games);
        }
    }
}
