using GameRecommendation.SteamImporter.Data;
using Moq;
using Moq.Protected;
using System.Net;

namespace GameRecommendation.SteamImporter.Tests
{
    public class SteamAppListSourceTests
    {
        private readonly Mock<HttpMessageHandler> handler = new();

        private SteamAppListSource CreateSource() => new(new HttpClient(handler.Object));

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

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAppIdsAsync_ReturnsAppIds_WhenResponseIsValid()
        {
            SetupResponse(HttpStatusCode.OK, """
            {
                "applist": {
                    "apps": [
                        { "appid": 730, "name": "CS2" },
                        { "appid": 570, "name": "Dota 2" }
                    ]
                }
            }
            """);

            var result = await CreateSource().GetAppIdsAsync();

            Assert.Equal(new[] { 730, 570 }, result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAppIdsAsync_ReturnsEmpty_WhenAppListIsEmpty()
        {
            SetupResponse(HttpStatusCode.OK, """{ "applist": { "apps": [] } }""");

            var result = await CreateSource().GetAppIdsAsync();

            Assert.Empty(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAppIdsAsync_Throws_WhenRequestFails()
        {
            SetupResponse(HttpStatusCode.InternalServerError);

            await Assert.ThrowsAsync<HttpRequestException>(() => CreateSource().GetAppIdsAsync());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAppIdsAsync_RequestsExpectedUrl()
        {
            HttpRequestMessage? capturedRequest = null;
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("""{ "applist": { "apps": [] } }""")
                });

            await CreateSource().GetAppIdsAsync();

            Assert.NotNull(capturedRequest);
            Assert.Equal(
                "https://api.steampowered.com/ISteamApps/GetAppList/v2/",
                capturedRequest!.RequestUri!.ToString());
        }
    }
}
