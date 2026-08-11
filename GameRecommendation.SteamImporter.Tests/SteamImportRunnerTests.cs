using GameRecommendation.Domain.Models.Domain;
using Moq;
using System.Text.Json;

namespace GameRecommendation.SteamImporter.Tests
{
    public class SteamImportRunnerTests
    {
        private static Game MakeGame(int steamAppId, string name = "Test Game") => new()
        {
            SteamAppId = steamAppId,
            Name = name,
            Description = "Test description",
            ImageUrl = "https://image.jpg",
            ReleaseDate = DateTime.UtcNow
        };

        private static JsonDocument MakeSteamResponse(int appId, bool success = true) =>
            JsonDocument.Parse($$"""
            {
                "{{appId}}": {
                    "success": {{(success ? "true" : "false")}},
                    "data": {
                        "name": "Test Game",
                        "short_description": "desc",
                        "header_image": "img",
                        "release_date": { "date": "2023-01-01" },
                        "genres": [],
                        "categories": []
                    }
                }
            }
            """);

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_AddsNewGame_WhenGameDoesNotExist()
        {
            var harness = new SteamImportTestHarness();
            var response = MakeSteamResponse(123);

            harness.Fetcher.Setup(f => f.GetGameAsync(123)).ReturnsAsync(response);
            harness.Mapper.Setup(m => m.Map(123, response)).Returns(MakeGame(123));
            harness.TagExtractor.Setup(e => e.Extract(It.IsAny<JsonElement>())).Returns([]);

            await harness.Runner.ImportGamesAsync(new[] { 123 });

            Assert.Single(harness.Database.Games);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_DoesNotDuplicate_WhenGameAlreadyExists()
        {
            var harness = new SteamImportTestHarness();
            harness.Database.Games.Add(MakeGame(123, "Existing Game"));
            await harness.Database.SaveChangesAsync(TestContext.Current.CancellationToken);

            var response = MakeSteamResponse(123);
            harness.Fetcher.Setup(f => f.GetGameAsync(123)).ReturnsAsync(response);
            harness.Mapper.Setup(m => m.Map(123, It.IsAny<JsonDocument>())).Returns(MakeGame(123, "Updated Game"));
            harness.TagExtractor.Setup(e => e.Extract(It.IsAny<JsonElement>())).Returns([]);

            await harness.Runner.ImportGamesAsync(new[] { 123 });

            Assert.Single(harness.Database.Games);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_UpdatesExistingGame_WhenGameAlreadyExists()
        {
            var harness = new SteamImportTestHarness();
            harness.Database.Games.Add(MakeGame(123, "Old Name"));
            await harness.Database.SaveChangesAsync(TestContext.Current.CancellationToken);

            var response = MakeSteamResponse(123);
            harness.Fetcher.Setup(f => f.GetGameAsync(123)).ReturnsAsync(response);
            harness.Mapper.Setup(m => m.Map(123, It.IsAny<JsonDocument>())).Returns(MakeGame(123, "New Name"));
            harness.TagExtractor.Setup(e => e.Extract(It.IsAny<JsonElement>())).Returns([]);

            await harness.Runner.ImportGamesAsync(new[] { 123 });

            Assert.Equal("New Name", harness.Database.Games.Single().Name);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_Skips_WhenFetcherReturnsNull()
        {
            var harness = new SteamImportTestHarness();

            harness.Fetcher.Setup(f => f.GetGameAsync(123)).ReturnsAsync((JsonDocument?)null);

            await harness.Runner.ImportGamesAsync(new[] { 123 });

            Assert.Empty(harness.Database.Games);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_Skips_WhenMapperReturnsNull()
        {
            var harness = new SteamImportTestHarness();
            var response = MakeSteamResponse(123, success: false);

            harness.Fetcher.Setup(f => f.GetGameAsync(123)).ReturnsAsync(response);
            harness.Mapper.Setup(m => m.Map(123, response)).Returns((Game?)null);

            await harness.Runner.ImportGamesAsync(new[] { 123 });

            Assert.Empty(harness.Database.Games);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_ImportsMultipleGames()
        {
            var harness = new SteamImportTestHarness();

            foreach (var appId in new[] { 111, 222, 333 })
            {
                var response = MakeSteamResponse(appId);
                harness.Fetcher.Setup(f => f.GetGameAsync(appId)).ReturnsAsync(response);
                harness.Mapper.Setup(m => m.Map(appId, response)).Returns(MakeGame(appId));
                harness.TagExtractor.Setup(e => e.Extract(It.IsAny<JsonElement>())).Returns([]);
            }

            await harness.Runner.ImportGamesAsync(new[] { 111, 222, 333 });

            Assert.Equal(3, harness.Database.Games.Count());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_AttachesTags_WhenTagsReturned()
        {
            var harness = new SteamImportTestHarness();
            var response = MakeSteamResponse(123);

            harness.Fetcher.Setup(f => f.GetGameAsync(123)).ReturnsAsync(response);
            harness.Mapper.Setup(m => m.Map(123, response)).Returns(MakeGame(123));
            harness.TagExtractor.Setup(e => e.Extract(It.IsAny<JsonElement>()))
                .Returns(new List<string> { "Action", "RPG" });

            await harness.Runner.ImportGamesAsync(new[] { 123 });

            var game = harness.Database.Games.Single();
            Assert.Equal(2, game.GameTags.Count);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_ReusesExistingTags_WhenTagAlreadyExists()
        {
            var harness = new SteamImportTestHarness();
            var response1 = MakeSteamResponse(111);
            var response2 = MakeSteamResponse(222);

            harness.Fetcher.Setup(f => f.GetGameAsync(111)).ReturnsAsync(response1);
            harness.Fetcher.Setup(f => f.GetGameAsync(222)).ReturnsAsync(response2);
            harness.Mapper.Setup(m => m.Map(111, response1)).Returns(MakeGame(111));
            harness.Mapper.Setup(m => m.Map(222, response2)).Returns(MakeGame(222));
            harness.TagExtractor.Setup(e => e.Extract(It.IsAny<JsonElement>()))
                .Returns(new List<string> { "Action" });

            await harness.Runner.ImportGamesAsync(new[] { 111, 222 });

            Assert.Single(harness.Database.Tags);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_DoesNothing_WhenAppIdListIsEmpty()
        {
            var harness = new SteamImportTestHarness();

            await harness.Runner.ImportGamesAsync(Array.Empty<int>());

            Assert.Empty(harness.Database.Games);
            harness.Fetcher.Verify(f => f.GetGameAsync(It.IsAny<int>()), Times.Never);
        }

        // ── Per-item failure isolation ───────────────────────────────────────
        // Regression tests: a single item throwing (e.g. a malformed Steam
        // response) must not abort the rest of the batch.

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_ContinuesBatch_WhenFetcherThrowsForOneAppId()
        {
            var harness = new SteamImportTestHarness();
            var goodResponse = MakeSteamResponse(222);

            harness.Fetcher.Setup(f => f.GetGameAsync(111)).ThrowsAsync(new InvalidOperationException("boom"));
            harness.Fetcher.Setup(f => f.GetGameAsync(222)).ReturnsAsync(goodResponse);
            harness.Mapper.Setup(m => m.Map(222, goodResponse)).Returns(MakeGame(222));
            harness.TagExtractor.Setup(e => e.Extract(It.IsAny<JsonElement>())).Returns([]);

            await harness.Runner.ImportGamesAsync(new[] { 111, 222 });

            Assert.Single(harness.Database.Games);
            Assert.Equal(222, harness.Database.Games.Single().SteamAppId);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_ContinuesBatch_WhenMapperThrowsForOneAppId()
        {
            var harness = new SteamImportTestHarness();
            var badResponse = MakeSteamResponse(111);
            var goodResponse = MakeSteamResponse(222);

            harness.Fetcher.Setup(f => f.GetGameAsync(111)).ReturnsAsync(badResponse);
            harness.Fetcher.Setup(f => f.GetGameAsync(222)).ReturnsAsync(goodResponse);
            harness.Mapper.Setup(m => m.Map(111, badResponse)).Throws(new KeyNotFoundException("missing field"));
            harness.Mapper.Setup(m => m.Map(222, goodResponse)).Returns(MakeGame(222));
            harness.TagExtractor.Setup(e => e.Extract(It.IsAny<JsonElement>())).Returns([]);

            await harness.Runner.ImportGamesAsync(new[] { 111, 222 });

            Assert.Single(harness.Database.Games);
            Assert.Equal(222, harness.Database.Games.Single().SteamAppId);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task ImportGamesAsync_StillSavesChanges_WhenAnItemThrows()
        {
            var harness = new SteamImportTestHarness();

            harness.Fetcher.Setup(f => f.GetGameAsync(111)).ThrowsAsync(new InvalidOperationException("boom"));

            await harness.Runner.ImportGamesAsync(new[] { 111 });

            // Should complete without propagating the exception to the caller.
            Assert.Empty(harness.Database.Games);
        }
    }
}
