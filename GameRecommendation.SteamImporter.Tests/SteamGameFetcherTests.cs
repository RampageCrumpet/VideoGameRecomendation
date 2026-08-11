using GameRecommendation.SteamImporter.Services;
using GameRecommendation.TestUtilities;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace GameRecommendation.SteamImporter.Tests
{
    public class SteamGameFetcherTests
    {
        private readonly FakeLogger<SteamGameFetcher> logger = new();
        private readonly Mock<HttpMessageHandler> handler = new();

        private SteamGameFetcher CreateFetcher() => new(new HttpClient(handler.Object), logger);

        private void SetupResponse(HttpStatusCode statusCode, string? content = null)
        {
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = content != null ? new StringContent(content) : new StringContent(string.Empty)
                });
        }

        private void SetupThrows(Exception exception)
        {
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(exception);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameAsync_ReturnsJsonDocument_WhenRequestSucceeds()
        {
            SetupResponse(HttpStatusCode.OK, """{ "730": { "success": true } }""");

            var result = await CreateFetcher().GetGameAsync(730);

            Assert.NotNull(result);
            Assert.True(result.RootElement.GetProperty("730").GetProperty("success").GetBoolean());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameAsync_ReturnsNull_WhenHttpRequestFails()
        {
            SetupResponse(HttpStatusCode.InternalServerError);

            var result = await CreateFetcher().GetGameAsync(730);

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameAsync_LogsError_WhenHttpRequestFails()
        {
            SetupResponse(HttpStatusCode.InternalServerError);

            await CreateFetcher().GetGameAsync(730);

            Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Error, logger.Entries[0].Level);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameAsync_ReturnsNull_WhenUnexpectedExceptionThrown()
        {
            SetupThrows(new InvalidOperationException("boom"));

            var result = await CreateFetcher().GetGameAsync(730);

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameAsync_LogsError_WhenUnexpectedExceptionThrown()
        {
            SetupThrows(new InvalidOperationException("boom"));

            await CreateFetcher().GetGameAsync(730);

            Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Error, logger.Entries[0].Level);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameAsync_RequestsExpectedUrl()
        {
            HttpRequestMessage? capturedRequest = null;
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") });

            await CreateFetcher().GetGameAsync(730);

            Assert.NotNull(capturedRequest);
            Assert.Equal(
                "https://store.steampowered.com/api/appdetails?appids=730&l=english",
                capturedRequest!.RequestUri!.ToString());
        }
    }
}
