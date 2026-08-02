using GameRecommendation.Infrastructure.Data;
using GameRecommendation.SteamImporter.Services;
using GameRecommendation.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameRecommendation.SteamImporter.Tests
{
    /// <summary>
    /// Shared test harness for <see cref="SteamImportRunner"/> tests.
    /// Wires up a real in-memory database alongside mocked dependencies.
    /// </summary>
    public class SteamImportTestHarness
    {
        /// <summary>The in-memory database used during the test.</summary>
        public TestRecommendationDbContext Database { get; }

        /// <summary>Mock for the Steam game fetcher.</summary>
        public Mock<ISteamGameFetcher> Fetcher { get; } = new();

        /// <summary>Mock for the Steam game mapper.</summary>
        public Mock<ISteamGameMapper> Mapper { get; } = new();

        /// <summary>Mock for the Steam tag extractor.</summary>
        public Mock<ISteamTagExtractor> TagExtractor { get; } = new();

        /// <summary>Mock for the logger.</summary>
        public Mock<ILogger<SteamImportRunner>> Logger { get; } = new();

        /// <summary>The <see cref="SteamImportRunner"/> under test.</summary>
        public SteamImportRunner Runner { get; }

        public SteamImportTestHarness()
        {
            var options = new DbContextOptionsBuilder<RecommendationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var testOptions = new DbContextOptionsBuilder<TestRecommendationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            Database = new TestRecommendationDbContext(testOptions);

            Runner = new SteamImportRunner(
                Fetcher.Object,
                Mapper.Object,
                TagExtractor.Object,
                Database,
                Logger.Object);
        }
    }
}
