using GameRecommendation.Web.Auth;
using GameRecommendation.Web.Services;
using System.Net;

namespace GameRecommendation.Web.Tests
{
    public class RecommendationsServiceTests
    {
        private readonly FakeJsRuntime js = new();

        private (RecommendationsService Service, FakeHttpMessageHandler Handler) CreateService(HttpStatusCode statusCode, string? content = null)
        {
            var handler = new FakeHttpMessageHandler(statusCode, content);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
            var authState = new JwtAuthenticationStateProvider(js);
            return (new RecommendationsService(httpClient, authState), handler);
        }

        private const string EmptyPage = """{ "items": [], "totalCount": 0, "pageSize": 20 }""";

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendationsAsync_ReturnsParsedResult_WhenSuccessful()
        {
            var (service, _) = CreateService(HttpStatusCode.OK, """
            {
                "items": [ { "id": 1, "name": "Recommended Game", "imageUrl": "https://img", "releaseDate": "2023-01-01", "score": 0.9 } ],
                "totalCount": 1,
                "pageSize": 20
            }
            """);

            var result = await service.GetRecommendationsAsync();

            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Recommended Game", result.Items.Single().Name);
            Assert.Equal(0.9, result.Items.Single().Score);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendationsAsync_ReturnsNull_WhenResponseUnsuccessful()
        {
            var (service, _) = CreateService(HttpStatusCode.InternalServerError);

            var result = await service.GetRecommendationsAsync();

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendationsAsync_IncludesPageAndPageSizeInQueryString()
        {
            var (service, handler) = CreateService(HttpStatusCode.OK, EmptyPage);

            await service.GetRecommendationsAsync(page: 2, pageSize: 15);

            var query = handler.LastRequest!.RequestUri!.Query;
            Assert.Contains("page=2", query);
            Assert.Contains("pageSize=15", query);
        }

        // ── Regression: this call was previously missing AttachTokenAsync() ────
        // entirely, unlike GamesService/RatingsService. It "worked" only because
        // AuthService happened to have set a default header earlier on the same
        // shared scoped HttpClient. On a fresh app load with a token already in
        // localStorage but AuthService never invoked this session, requests would
        // silently go out unauthenticated.

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendationsAsync_AttachesBearerToken_FromAuthStateProvider()
        {
            js.SeedToken("stored.jwt.token");
            var (service, handler) = CreateService(HttpStatusCode.OK, EmptyPage);

            await service.GetRecommendationsAsync();

            Assert.Equal("stored.jwt.token", handler.LastRequest!.Headers.Authorization?.Parameter);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendationsAsync_SendsNoAuthorizationHeader_WhenNoTokenStored()
        {
            var (service, handler) = CreateService(HttpStatusCode.OK, EmptyPage);

            await service.GetRecommendationsAsync();

            Assert.Null(handler.LastRequest!.Headers.Authorization);
        }
    }
}
