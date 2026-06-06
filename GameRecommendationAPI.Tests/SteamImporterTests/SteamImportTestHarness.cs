using GameRecommendation.Infrastructure;
using GameRecommendation.SteamImporter.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GameRecommendation.Tests.SteamImporterTests
{
    public class SteamImportTestHarness
    {
        public RecommendationDbContext Database { get; }

        public Mock<ISteamGameFetcher> Fetcher { get; } = new();
        public Mock<ISteamGameMapper> Mapper { get; } = new();
        public Mock<ISteamTagExtractor> TagExtractor { get; } = new();

        public SteamImportRunner Runner { get; }

        public SteamImportTestHarness()
        {
            var options = new DbContextOptionsBuilder<RecommendationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            Database = new RecommendationDbContext(options);

            Runner = new SteamImportRunner(
                Fetcher.Object,
                Mapper.Object,
                TagExtractor.Object,
                Database);
        }
    }
}
