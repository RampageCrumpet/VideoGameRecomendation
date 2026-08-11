using GameRecommendation.Web.Auth;
using GameRecommendation.Web.Services;
using System.Net;

namespace GameRecommendation.Web.Tests
{
    public class GamesServiceTests
    {
        private readonly FakeJsRuntime js = new();

        private (GamesService Service, FakeHttpMessageHandler Handler) CreateService(HttpStatusCode statusCode, string? content = null)
        {
            var handler = new FakeHttpMessageHandler(statusCode, content);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
            var authState = new JwtAuthenticationStateProvider(js);
            return (new GamesService(httpClient, authState), handler);
        }

        private const string EmptyPage = """{ "items": [], "totalCount": 0, "pageSize": 20 }""";

        // ── GetGamesAsync: URL building ──────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGamesAsync_OmitsSearchParam_WhenSearchIsNull()
        {
            var (service, handler) = CreateService(HttpStatusCode.OK, EmptyPage);

            await service.GetGamesAsync(search: null);

            Assert.DoesNotContain("search=", handler.LastRequest!.RequestUri!.Query);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGamesAsync_OmitsSearchParam_WhenSearchIsWhitespace()
        {
            var (service, handler) = CreateService(HttpStatusCode.OK, EmptyPage);

            await service.GetGamesAsync(search: "   ");

            Assert.DoesNotContain("search=", handler.LastRequest!.RequestUri!.Query);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGamesAsync_IncludesEscapedSearchParam_WhenSearchProvided()
        {
            var (service, handler) = CreateService(HttpStatusCode.OK, EmptyPage);

            await service.GetGamesAsync(search: "counter strike");

            Assert.Contains("search=counter%20strike", handler.LastRequest!.RequestUri!.Query);
        }

        [Theory]
        [Trait("Category", "Unit")]
        [InlineData(true, "ratedOnly=true")]
        [InlineData(false, "ratedOnly=false")]
        public async Task GetGamesAsync_LowercasesRatedOnlyInQueryString(bool ratedOnly, string expectedFragment)
        {
            var (service, handler) = CreateService(HttpStatusCode.OK, EmptyPage);

            await service.GetGamesAsync(ratedOnly: ratedOnly);

            Assert.Contains(expectedFragment, handler.LastRequest!.RequestUri!.Query);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGamesAsync_IncludesPageAndPageSizeInQueryString()
        {
            var (service, handler) = CreateService(HttpStatusCode.OK, EmptyPage);

            await service.GetGamesAsync(page: 3, pageSize: 7);

            var query = handler.LastRequest!.RequestUri!.Query;
            Assert.Contains("page=3", query);
            Assert.Contains("pageSize=7", query);
        }

        // ── GetGamesAsync: response handling ─────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGamesAsync_ReturnsNull_WhenResponseUnsuccessful()
        {
            var (service, _) = CreateService(HttpStatusCode.InternalServerError);

            var result = await service.GetGamesAsync();

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGamesAsync_ReturnsParsedResult_WhenSuccessful()
        {
            var (service, _) = CreateService(HttpStatusCode.OK, """
            {
                "items": [ { "id": 1, "name": "Test Game", "imageUrl": "https://img", "releaseDate": "2023-01-01" } ],
                "totalCount": 1,
                "pageSize": 20
            }
            """);

            var result = await service.GetGamesAsync();

            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Test Game", result.Items.Single().Name);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGamesAsync_AttachesBearerToken_FromAuthStateProvider()
        {
            js.SeedToken("stored.jwt.token");
            var (service, handler) = CreateService(HttpStatusCode.OK, EmptyPage);

            await service.GetGamesAsync();

            Assert.Equal("stored.jwt.token", handler.LastRequest!.Headers.Authorization?.Parameter);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGamesAsync_SendsNoAuthorizationHeader_WhenNoTokenStored()
        {
            var (service, handler) = CreateService(HttpStatusCode.OK, EmptyPage);

            await service.GetGamesAsync();

            Assert.Null(handler.LastRequest!.Headers.Authorization);
        }

        // ── GetUnratedGameAsync ───────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetUnratedGameAsync_ReturnsNull_On204NoContent()
        {
            var (service, _) = CreateService(HttpStatusCode.NoContent);

            var result = await service.GetUnratedGameAsync();

            Assert.Null(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetUnratedGameAsync_ReturnsGame_WhenSuccessful()
        {
            var (service, _) = CreateService(HttpStatusCode.OK, """
            { "id": 5, "name": "Unrated Game", "description": "desc", "imageUrl": "https://img", "releaseDate": "2023-01-01", "tags": [] }
            """);

            var result = await service.GetUnratedGameAsync();

            Assert.NotNull(result);
            Assert.Equal(5, result.Id);
            Assert.Equal("Unrated Game", result.Name);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetUnratedGameAsync_ReturnsNull_WhenUnsuccessful()
        {
            var (service, _) = CreateService(HttpStatusCode.InternalServerError);

            var result = await service.GetUnratedGameAsync();

            Assert.Null(result);
        }

        // ── GetGameAsync ──────────────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameAsync_ReturnsGame_WhenFound()
        {
            var (service, _) = CreateService(HttpStatusCode.OK, """
            { "id": 1, "name": "Game 1", "description": "desc", "imageUrl": "https://img", "releaseDate": "2023-01-01", "tags": ["Action"] }
            """);

            var result = await service.GetGameAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Game 1", result.Name);
            Assert.Contains("Action", result.Tags);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetGameAsync_Throws_WhenGameNotFound()
        {
            // Documents current behavior: unlike GetGamesAsync/GetUnratedGameAsync (which check
            // IsSuccessStatusCode and return null), GetGameAsync uses GetFromJsonAsync directly,
            // which throws HttpRequestException on a non-success response instead of returning null.
            var (service, _) = CreateService(HttpStatusCode.NotFound);

            await Assert.ThrowsAsync<HttpRequestException>(() => service.GetGameAsync(999));
        }
    }
}
