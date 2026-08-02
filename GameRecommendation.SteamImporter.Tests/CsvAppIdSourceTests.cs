using GameRecommendation.SteamImporter.Data;

namespace GameRecommendation.SteamImporter.Tests
{
    public class CsvAppIdSourceTests
    {
        private static async Task<CsvAppIdSource> CreateWithContent(params string[] lines)
        {
            var path = Path.GetTempFileName();
            await File.WriteAllLinesAsync(path, lines);
            return new CsvAppIdSource(path);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAppIdsAsync_ReturnsIds_FromValidFile()
        {
            var source = await CreateWithContent("730", "570", "440");

            var result = await source.GetAppIdsAsync();

            Assert.Equal(new[] { 730, 570, 440 }, result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAppIdsAsync_SkipsBlankLines()
        {
            var source = await CreateWithContent("730", "", "570", "   ", "440");

            var result = await source.GetAppIdsAsync();

            Assert.Equal(new[] { 730, 570, 440 }, result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAppIdsAsync_SkipsCommentLines()
        {
            var source = await CreateWithContent("# this is a comment", "730", "# another comment", "570");

            var result = await source.GetAppIdsAsync();

            Assert.Equal(new[] { 730, 570 }, result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAppIdsAsync_SkipsNonNumericLines()
        {
            var source = await CreateWithContent("AppId", "730", "not-a-number", "570");

            var result = await source.GetAppIdsAsync();

            Assert.Equal(new[] { 730, 570 }, result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAppIdsAsync_ReturnsEmpty_WhenFileIsEmpty()
        {
            var source = await CreateWithContent();

            var result = await source.GetAppIdsAsync();

            Assert.Empty(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAppIdsAsync_TrimsWhitespace_AroundIds()
        {
            var source = await CreateWithContent("  730  ", " 570", "440 ");

            var result = await source.GetAppIdsAsync();

            Assert.Equal(new[] { 730, 570, 440 }, result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAppIdsAsync_ReturnsEmpty_WhenFileContainsOnlyComments()
        {
            var source = await CreateWithContent("# comment one", "# comment two");

            var result = await source.GetAppIdsAsync();

            Assert.Empty(result);
        }
    }
}
