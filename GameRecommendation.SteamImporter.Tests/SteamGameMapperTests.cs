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

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsGame_WhenJsonIsValid()
        {
            var appId = 12345;
            var json = $@"
            {{
                ""{appId}"": {{
                    ""success"": true,
                    ""data"": {{
                        ""name"": ""Test Game"",
                        ""short_description"": ""Short desc"",
                        ""header_image"": ""http://image"",
                        ""release_date"": {{ ""date"": ""Apr 1, 2020"" }}
                    }}
                }}
            }}";

            using var doc = JsonDocument.Parse(json);

            var game = mapper.Map(appId, doc);

            Assert.NotNull(game);
            Assert.Equal(appId, game!.SteamAppId);
            Assert.Equal("Test Game", game.Name);
            Assert.Equal("Short desc", game.Description);
            Assert.Equal("http://image", game.ImageUrl);
            Assert.Equal(DateTime.Parse("Apr 1, 2020"), game.ReleaseDate);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsNull_WhenAppIdMissing()
        {
            var appId = 12345;
            var json = @"{ ""99999"": { ""success"": true, ""data"": {} } }";

            using var doc = JsonDocument.Parse(json);

            var result = mapper.Map(appId, doc);

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsNull_WhenSuccessIsFalse()
        {
            var appId = 12345;
            var json = $@"{{ ""{appId}"": {{ ""success"": false }} }}";

            using var doc = JsonDocument.Parse(json);

            var result = mapper.Map(appId, doc);

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_ReturnsNull_WhenDataPropertyMissing()
        {
            var appId = 12345;
            var json = $@"{{ ""{appId}"": {{ ""success"": true }} }}";

            using var doc = JsonDocument.Parse(json);

            var result = mapper.Map(appId, doc);

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Map_UnparseableReleaseDate_ReturnsMinValue()
        {
            var appId = 12345;
            var json = $@"
            {{
                ""{appId}"": {{
                    ""success"": true,
                    ""data"": {{
                        ""name"": ""Test Game"",
                        ""short_description"": ""Short desc"",
                        ""header_image"": ""http://image"",
                        ""release_date"": {{ ""date"": ""not-a-date"" }}
                    }}
                }}
            }}";

            using var doc = JsonDocument.Parse(json);

            var game = mapper.Map(appId, doc);

            Assert.NotNull(game);
            Assert.Equal(DateTime.MinValue, game!.ReleaseDate);
        }
    }
}