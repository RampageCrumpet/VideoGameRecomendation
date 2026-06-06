using GameRecommendation.SteamImporter.Services;
using System.Text.Json;

namespace GameRecommendation.Tests.SteamImporterTests
{
    public class SteamGameMapperTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsGame_WhenSteamResponseIsValid()
        {
            var mapper = new SteamGameMapper();

            var json = JsonDocument.Parse("""
            {
                "730":
                {
                    "success": true,
                    "data":
                    {
                        "name": "Counter-Strike 2",
                        "short_description": "Test Description",
                        "header_image": "https://image.jpg"
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
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsNull_WhenSteamReturnsFailure()
        {
            var mapper = new SteamGameMapper();

            var json = JsonDocument.Parse("""
            {
                "730":
                {
                    "success": false
                }
            }
            """);

            var result = mapper.Map(730, json);

            Assert.Null(result);
        }
    }
}
