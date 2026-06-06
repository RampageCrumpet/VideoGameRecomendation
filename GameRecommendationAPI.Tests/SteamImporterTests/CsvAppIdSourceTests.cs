using GameRecommendation.SteamImporter.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameRecommendation.Tests.SteamImporterTests
{
    public class CsvAppIdSourceTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAppIdsAsync_ReadsIdsFromFile()
        {
            var path = Path.GetTempFileName();

            await File.WriteAllLinesAsync(path, ["730", "570", "440"], TestContext.Current.CancellationToken);

            var source = new CsvAppIdSource(path);

            var result = await source.GetAppIdsAsync();

            Assert.Equal(new[] { 730, 570, 440 }, result);
        }
    }
}
