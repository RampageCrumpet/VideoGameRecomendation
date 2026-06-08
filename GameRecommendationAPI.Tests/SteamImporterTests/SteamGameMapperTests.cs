using GameRecommendation.SteamImporter.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace GameRecommendation.SteamImporter.Tests
{
    public class SteamGameMapperTests
    {
        private readonly SteamGameMapper mapper;

        public SteamGameMapperTests()
        {
            var logger = new Mock<ILogger<SteamGameMapper>>();
            mapper = new SteamGameMapper(logger.Object);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsGame_WhenSteamResponseIsValid()
        {
            var json = JsonDocument.Parse("""
            {
                "730": {
                    "success": true,
                    "data": {
                        "name": "Counter-Strike 2",
                        "short_description": "Test Description",
                        "header_image": "https://image.jpg",
                        "release_date": { "date": "2023-06-01" }
                    }
                }
            }
            """);

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
        public void Map_ReturnsNull_WhenSteamReturnsFailure()
        {
            var json = JsonDocument.Parse("""
            {
                "730": {
                    "success": false
                }
            }
            """);

            var result = mapper.Map(730, json);

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsNull_WhenAppIdNotInResponse()
        {
            var json = JsonDocument.Parse("""
            {
                "999": {
                    "success": true,
                    "data": {}
                }
            }
            """);

            var result = mapper.Map(730, json);

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsMinDate_WhenReleaseDateUnparseable()
        {
            var json = JsonDocument.Parse("""
            {
                "730": {
                    "success": true,
                    "data": {
                        "name": "Counter-Strike 2",
                        "short_description": "Test Description",
                        "header_image": "https://image.jpg",
                        "release_date": { "date": "coming soon" }
                    }
                }
            }
            """);

            var result = mapper.Map(730, json);

            Assert.NotNull(result);
            Assert.Equal(DateTime.MinValue, result.ReleaseDate);
        }
    }
}