using GameRecommendation.SteamImporter.Services;
using System.Text.Json;

namespace GameRecommendation.SteamImporter.Tests
{
    public class SteamTagExtractorTests
    {
        private readonly SteamTagExtractor extractor = new();

        private static JsonElement Parse(string json)
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Extract_ReturnsTagsFromGenresAndCategories()
        {
            var element = Parse("""
            {
                "genres": [
                    { "description": "Action" },
                    { "description": "Adventure" }
                ],
                "categories": [
                    { "description": "Co-op" }
                ]
            }
            """);

            var tags = extractor.Extract(element).ToList();

            Assert.Contains("Action", tags);
            Assert.Contains("Adventure", tags);
            Assert.Contains("Co-op", tags);
            Assert.Equal(3, tags.Count);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Extract_DeduplicatesTags()
        {
            var element = Parse("""
            {
                "genres": [
                    { "description": "Action" },
                    { "description": "Action" }
                ],
                "categories": [
                    { "description": "Action" }
                ]
            }
            """);

            var tags = extractor.Extract(element).ToList();

            Assert.Single(tags);
            Assert.Equal("Action", tags[0]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Extract_IgnoresWhitespaceOnlyEntries()
        {
            var element = Parse("""
            {
                "genres": [
                    { "description": "   " },
                    { "description": "Action" }
                ],
                "categories": []
            }
            """);

            var tags = extractor.Extract(element).ToList();

            Assert.Single(tags);
            Assert.Equal("Action", tags[0]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Extract_ReturnsEmpty_WhenNoRelevantProperties()
        {
            var element = Parse("{}");

            var tags = extractor.Extract(element);

            Assert.Empty(tags);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Extract_IgnoresItemsWithoutDescriptionProperty()
        {
            var element = Parse("""
            {
                "genres": [
                    { "id": 1 },
                    { "description": "Strategy" }
                ],
                "categories": [
                    { "somethingElse": "x" }
                ]
            }
            """);

            var tags = extractor.Extract(element).ToList();

            Assert.Single(tags);
            Assert.Equal("Strategy", tags[0]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Extract_ReturnsEmpty_WhenGenresAndCategoriesAreEmpty()
        {
            var element = Parse("""
            {
                "genres": [],
                "categories": []
            }
            """);

            var tags = extractor.Extract(element);

            Assert.Empty(tags);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Extract_ReturnsOnlyGenres_WhenCategoriesMissing()
        {
            var element = Parse("""
            {
                "genres": [
                    { "description": "RPG" }
                ]
            }
            """);

            var tags = extractor.Extract(element).ToList();

            Assert.Single(tags);
            Assert.Equal("RPG", tags[0]);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Extract_ReturnsOnlyCategories_WhenGenresMissing()
        {
            var element = Parse("""
            {
                "categories": [
                    { "description": "Multiplayer" }
                ]
            }
            """);

            var tags = extractor.Extract(element).ToList();

            Assert.Single(tags);
            Assert.Equal("Multiplayer", tags[0]);
        }
    }
}
