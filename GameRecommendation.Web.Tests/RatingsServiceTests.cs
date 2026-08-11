using GameRecommendation.Domain.Enums;
using GameRecommendation.Web.Auth;
using GameRecommendation.Web.Services;
using System.Net;
using System.Text.Json;

namespace GameRecommendation.Web.Tests
{
    public class RatingsServiceTests
    {
        private readonly FakeJsRuntime js = new();

        private (RatingsService Service, FakeHttpMessageHandler Handler) CreateService(HttpStatusCode statusCode)
        {
            var handler = new FakeHttpMessageHandler(statusCode);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
            var authState = new JwtAuthenticationStateProvider(js);
            return (new RatingsService(httpClient, authState), handler);
        }

        // ── UpsertRatingAsync ─────────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UpsertRatingAsync_ReturnsTrue_WhenSuccessful()
        {
            var (service, _) = CreateService(HttpStatusCode.NoContent);

            var result = await service.UpsertRatingAsync(1, RatingType.Like);

            Assert.True(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UpsertRatingAsync_ReturnsFalse_WhenUnsuccessful()
        {
            var (service, _) = CreateService(HttpStatusCode.NotFound);

            var result = await service.UpsertRatingAsync(999, RatingType.Like);

            Assert.False(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UpsertRatingAsync_SendsGameIdAndRating_InRequestBody()
        {
            var (service, handler) = CreateService(HttpStatusCode.NoContent);

            await service.UpsertRatingAsync(42, RatingType.Dislike);

            var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);
            Assert.Equal(42, json.RootElement.GetProperty("gameId").GetInt32());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UpsertRatingAsync_SendsPutRequest_ToRatingsEndpoint()
        {
            var (service, handler) = CreateService(HttpStatusCode.NoContent);

            await service.UpsertRatingAsync(1, RatingType.Like);

            Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
            Assert.Equal("/api/ratings", handler.LastRequest.RequestUri!.AbsolutePath);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UpsertRatingAsync_AttachesBearerToken_FromAuthStateProvider()
        {
            js.SeedToken("stored.jwt.token");
            var (service, handler) = CreateService(HttpStatusCode.NoContent);

            await service.UpsertRatingAsync(1, RatingType.Like);

            Assert.Equal("stored.jwt.token", handler.LastRequest!.Headers.Authorization?.Parameter);
        }

        // ── DeleteRatingAsync ─────────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task DeleteRatingAsync_ReturnsTrue_WhenSuccessful()
        {
            var (service, _) = CreateService(HttpStatusCode.NoContent);

            var result = await service.DeleteRatingAsync(1);

            Assert.True(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task DeleteRatingAsync_ReturnsFalse_WhenUnsuccessful()
        {
            var (service, _) = CreateService(HttpStatusCode.NotFound);

            var result = await service.DeleteRatingAsync(999);

            Assert.False(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task DeleteRatingAsync_RequestsCorrectUrl()
        {
            var (service, handler) = CreateService(HttpStatusCode.NoContent);

            await service.DeleteRatingAsync(7);

            Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
            Assert.Equal("/api/ratings/7", handler.LastRequest.RequestUri!.AbsolutePath);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task DeleteRatingAsync_AttachesBearerToken_FromAuthStateProvider()
        {
            js.SeedToken("stored.jwt.token");
            var (service, handler) = CreateService(HttpStatusCode.NoContent);

            await service.DeleteRatingAsync(1);

            Assert.Equal("stored.jwt.token", handler.LastRequest!.Headers.Authorization?.Parameter);
        }
    }
}
