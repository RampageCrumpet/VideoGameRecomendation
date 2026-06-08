using GameRecommendation.SteamImporter.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace GameRecommendation.SteamImporter.Tests
{
    public class SteamTagExtractorTests
    {
        private SteamTagExtractor Create() => new SteamTagExtractor();

        [Fact]
        [Trait("Category", "Unit")]
        public void Extract_ReturnsTagsFromGenresAndCategories()
        {
            var json = @"
            {
                ""genres"": [
                    { ""description"": ""Action"" },
                    { ""description"": ""Adventure"" }
                ],
                ""categories"": [
                    { ""description"": ""Co-op"" }
                ]
            }";

            using var doc = JsonDocument.Parse(json);
            var extractor = Create();

            var tags = extractor.Extract(doc.RootElement).ToList();

            Assert.Contains("Action", tags);
            Assert.Contains("Adventure", tags);
            Assert.Contains("Co-op", tags);
            Assert.Equal(3, tags.Count);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Extract_IgnoresDuplicatesAndWhitespace()
        {
            var json = @"
            {
                ""genres"": [
                    { ""description"": ""Action"" },
                    { ""description"": ""Action"" },
                    { ""description"": ""  "" }
                ],
                ""categories"": [
                    { ""description"": ""Action"" },
                    { ""description"": ""Co-op"" }
                ]
            }";

            using var doc = JsonDocument.Parse(json);
            var extractor = Create();

            var tags = extractor.Extract(doc.RootElement).ToList();

            Assert.Contains("Action", tags);
            Assert.Contains("Co-op", tags);
            Assert.Equal(2, tags.Count);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Extract_ReturnsEmpty_WhenNoRelevantProperties()
        {
            var json = @"{ }";

            using var doc = JsonDocument.Parse(json);
            var extractor = Create();

            var tags = extractor.Extract(doc.RootElement);

            Assert.Empty(tags);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Extract_IgnoresItemsWithoutDescription()
        {
            var json = @"
            {
                ""genres"": [
                    { ""id"": 1 },
                    { ""description"": ""Strategy"" }
                ],
                ""categories"": [
                    { ""somethingElse"": ""x"" }
                ]
            }";

            using var doc = JsonDocument.Parse(json);
            var extractor = Create();

            var tags = extractor.Extract(doc.RootElement).ToList();

            Assert.Contains("Strategy", tags);
            Assert.Single(tags);
        }
    }
}
