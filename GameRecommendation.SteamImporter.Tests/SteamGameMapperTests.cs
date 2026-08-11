using GameRecommendation.SteamImporter.Services;
using GameRecommendation.TestUtilities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameRecommendation.SteamImporter.Tests
{
    public class SteamGameMapperTests
    {
        private readonly FakeLogger<SteamGameMapper> logger = new();
        private readonly SteamGameMapper mapper;

        public SteamGameMapperTests()
        {
            mapper = new SteamGameMapper(logger);
        }

        private static JsonDocument ValidResponse(int appId, string name = "Test Game",
            string description = "Test Description", string image = "https://image.jpg",
            string releaseDate = "2023-06-01") => JsonDocument.Parse($$"""
            {
                "{{appId}}": {
                    "success": true,
                    "data": {
                        "name": "{{name}}",
                        "short_description": "{{description}}",
                        "header_image": "{{image}}",
                        "release_date": { "date": "{{releaseDate}}" }
                    }
                }
            }
            """);

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsGame_WhenResponseIsValid()
        {
            using var json = ValidResponse(730, "Counter-Strike 2", "Test Description",
                "https://image.jpg", "2023-06-01");

            var result = mapper.Map(730, json);

            Assert.NotNull(result);
            Assert.Equal(730, result.SteamAppId);
            Assert.Equal("Counter-Strike 2", result.Name);
            Assert.Equal("Test Description", result.Description);
            Assert.Equal("https://image.jpg", result.ImageUrl);
            Assert.Equal(new DateTime(2023, 6, 1), result.ReleaseDate);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsNull_WhenSuccessIsFalse()
        {
            using var json = JsonDocument.Parse("""{ "730": { "success": false } }""");

            var result = mapper.Map(730, json);

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsNull_WhenAppIdNotInResponse()
        {
            using var json = JsonDocument.Parse("""{ "999": { "success": true, "data": {} } }""");

            var result = mapper.Map(730, json);

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsNull_WhenDataPropertyMissing()
        {
            using var json = JsonDocument.Parse("""{ "730": { "success": true } }""");

            var result = mapper.Map(730, json);

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsMinDate_WhenReleaseDateUnparseable()
        {
            using var json = ValidResponse(730, releaseDate: "coming soon");

            var result = mapper.Map(730, json);

            Assert.NotNull(result);
            Assert.Equal(DateTime.MinValue, result.ReleaseDate);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_PreservesUnicodeCharacters()
        {
            using var json = ValidResponse(730, name: "Call of Duty®", description: "Test™ Description");

            var result = mapper.Map(730, json);

            Assert.NotNull(result);
            Assert.Equal("Call of Duty®", result.Name);
            Assert.Equal("Test™ Description", result.Description);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_DoesNotLog_WhenMappingSucceeds()
        {
            using var json = ValidResponse(730);

            mapper.Map(730, json);

            Assert.Empty(logger.Entries);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_LogsWarning_WhenReleaseDateUnparseable()
        {
            using var json = ValidResponse(730, releaseDate: "coming soon");

            mapper.Map(730, json);

            Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Warning, logger.Entries[0].Level);
            Assert.Contains("730", logger.Entries[0].Message);
            Assert.Contains("coming soon", logger.Entries[0].Message);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_LogsWarning_WhenAppIdNotInResponse()
        {
            using var json = JsonDocument.Parse("""{ "999": { "success": true, "data": {} } }""");

            mapper.Map(730, json);

            Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Warning, logger.Entries[0].Level);
            Assert.Contains("730", logger.Entries[0].Message);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_LogsWarning_WhenSuccessIsFalse()
        {
            using var json = JsonDocument.Parse("""{ "730": { "success": false } }""");

            mapper.Map(730, json);

            Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Warning, logger.Entries[0].Level);
        }

        // ── Missing field resilience ─────────────────────────────────────────
        // Regression tests: Map() must never throw on a malformed-but-"successful"
        // Steam response. It previously used GetProperty (which throws on a missing
        // key) for name/short_description/header_image/release_date.

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_DoesNotThrow_WhenNamePropertyMissing()
        {
            using var json = JsonDocument.Parse("""
            {
                "730": {
                    "success": true,
                    "data": {
                        "short_description": "desc",
                        "header_image": "img",
                        "release_date": { "date": "2023-01-01" }
                    }
                }
            }
            """);

            var result = mapper.Map(730, json);

            Assert.NotNull(result);
            Assert.Equal("", result.Name);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_DoesNotThrow_WhenShortDescriptionPropertyMissing()
        {
            using var json = JsonDocument.Parse("""
            {
                "730": {
                    "success": true,
                    "data": {
                        "name": "Test Game",
                        "header_image": "img",
                        "release_date": { "date": "2023-01-01" }
                    }
                }
            }
            """);

            var result = mapper.Map(730, json);

            Assert.NotNull(result);
            Assert.Equal("", result.Description);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_DoesNotThrow_WhenHeaderImagePropertyMissing()
        {
            using var json = JsonDocument.Parse("""
            {
                "730": {
                    "success": true,
                    "data": {
                        "name": "Test Game",
                        "short_description": "desc",
                        "release_date": { "date": "2023-01-01" }
                    }
                }
            }
            """);

            var result = mapper.Map(730, json);

            Assert.NotNull(result);
            Assert.Equal("", result.ImageUrl);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_DoesNotThrow_WhenReleaseDatePropertyEntirelyMissing()
        {
            using var json = JsonDocument.Parse("""
            {
                "730": {
                    "success": true,
                    "data": {
                        "name": "Test Game",
                        "short_description": "desc",
                        "header_image": "img"
                    }
                }
            }
            """);

            var result = mapper.Map(730, json);

            Assert.NotNull(result);
            Assert.Equal(DateTime.MinValue, result.ReleaseDate);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_LogsWarning_WhenReleaseDatePropertyEntirelyMissing()
        {
            using var json = JsonDocument.Parse("""
            {
                "730": {
                    "success": true,
                    "data": {
                        "name": "Test Game",
                        "short_description": "desc",
                        "header_image": "img"
                    }
                }
            }
            """);

            mapper.Map(730, json);

            Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Warning, logger.Entries[0].Level);
            Assert.Contains("missing", logger.Entries[0].Message);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsGame_WhenAllOptionalFieldsMissing()
        {
            using var json = JsonDocument.Parse("""{ "730": { "success": true, "data": {} } }""");

            var result = mapper.Map(730, json);

            Assert.NotNull(result);
            Assert.Equal(730, result.SteamAppId);
            Assert.Equal("", result.Name);
            Assert.Equal("", result.Description);
            Assert.Equal("", result.ImageUrl);
            Assert.Equal(DateTime.MinValue, result.ReleaseDate);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsEmptyString_WhenNamePropertyIsJsonNull()
        {
            using var json = JsonDocument.Parse("""
            {
                "730": {
                    "success": true,
                    "data": {
                        "name": null,
                        "short_description": "desc",
                        "header_image": "img",
                        "release_date": { "date": "2023-01-01" }
                    }
                }
            }
            """);

            var result = mapper.Map(730, json);

            Assert.NotNull(result);
            Assert.Equal("", result.Name);
        }
    }
}
