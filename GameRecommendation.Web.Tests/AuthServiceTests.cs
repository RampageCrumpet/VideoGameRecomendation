using GameRecommendation.Web.Auth;
using GameRecommendation.Web.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GameRecommendation.Web.Tests
{
    public class AuthServiceTests
    {
        private readonly FakeJsRuntime js = new();

        private (AuthService Service, FakeHttpMessageHandler Handler, JwtAuthenticationStateProvider AuthState, HttpClient HttpClient) CreateService(
            HttpStatusCode statusCode, string? content = null)
        {
            var handler = new FakeHttpMessageHandler(statusCode, content);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
            var authState = new JwtAuthenticationStateProvider(js);
            return (new AuthService(httpClient, authState), handler, authState, httpClient);
        }

        /// <summary>
        /// Builds a syntactically valid (unsigned) JWT -- three dot-separated segments whose middle
        /// segment is real base64url-encoded JSON. AuthService always feeds the "token" from a
        /// successful response into JwtAuthenticationStateProvider.NotifyUserAuthenticated, which
        /// base64-decodes that middle segment and parses it as JSON. A placeholder like "abc.def.ghi"
        /// LOOKS like a JWT but isn't one -- "def" doesn't decode to valid JSON, so it blows up
        /// exactly the same way a real corrupted token would.
        /// </summary>
        private static string BuildJwt(string subject = "user1")
        {
            var payloadJson = JsonSerializer.Serialize(new { sub = subject });
            var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            return $"header.{payloadBase64}.signature";
        }

        private static string SuccessBody(string? token = null, string userName = "newuser") =>
            JsonSerializer.Serialize(new
            {
                token = token ?? BuildJwt(),
                expiresUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                userName
            });

        // ── RegisterAsync ─────────────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task RegisterAsync_ReturnsSuccess_WhenApiReturnsCreated()
        {
            var (service, _, _, _) = CreateService(HttpStatusCode.Created, SuccessBody());

            var (success, error) = await service.RegisterAsync("newuser", "new@example.com", "Test1234A");

            Assert.True(success);
            Assert.Null(error);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task RegisterAsync_StoresToken_WhenApiReturnsCreated()
        {
            var token = BuildJwt("newuser");
            var (service, _, authState, _) = CreateService(HttpStatusCode.Created, SuccessBody(token));

            await service.RegisterAsync("newuser", "new@example.com", "Test1234A");

            Assert.Equal(token, await authState.GetToken());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task RegisterAsync_SetsDefaultAuthorizationHeader_OnHttpClient()
        {
            var token = BuildJwt("newuser");
            var (service, _, _, httpClient) = CreateService(HttpStatusCode.Created, SuccessBody(token));

            await service.RegisterAsync("newuser", "new@example.com", "Test1234A");

            Assert.Equal("Bearer", httpClient.DefaultRequestHeaders.Authorization?.Scheme);
            Assert.Equal(token, httpClient.DefaultRequestHeaders.Authorization?.Parameter);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task RegisterAsync_ReturnsFriendlyError_WhenValidationErrorsReturned()
        {
            var (service, _, _, _) = CreateService(HttpStatusCode.BadRequest,
                """{ "errors": { "Password": ["Password too short."] } }""");

            var (success, error) = await service.RegisterAsync("newuser", "new@example.com", "short");

            Assert.False(success);
            Assert.Equal("Password too short.", error);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task RegisterAsync_ReturnsFriendlyError_WhenProblemDetailsTitleReturned()
        {
            var (service, _, _, _) = CreateService(HttpStatusCode.BadRequest,
                """{ "title": "One or more validation errors occurred." }""");

            var (success, error) = await service.RegisterAsync("newuser", "new@example.com", "Test1234A");

            Assert.False(success);
            Assert.Equal("One or more validation errors occurred.", error);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task RegisterAsync_ReturnsFriendlyError_WhenIdentityErrorArrayReturned()
        {
            var (service, _, _, _) = CreateService(HttpStatusCode.BadRequest,
                """[{ "code": "DuplicateEmail", "description": "Email 'new@example.com' is already taken." }]""");

            var (success, error) = await service.RegisterAsync("newuser", "new@example.com", "Test1234A");

            Assert.False(success);
            Assert.Equal("Email 'new@example.com' is already taken.", error);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task RegisterAsync_ReturnsGenericError_WhenBodyIsEmpty()
        {
            var (service, _, _, _) = CreateService(HttpStatusCode.BadRequest, "");

            var (success, error) = await service.RegisterAsync("newuser", "new@example.com", "Test1234A");

            Assert.False(success);
            Assert.Equal("An error occurred.", error);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task RegisterAsync_ReturnsRawContent_WhenBodyIsNotJson()
        {
            var (service, _, _, _) = CreateService(HttpStatusCode.InternalServerError, "Internal Server Error");

            var (success, error) = await service.RegisterAsync("newuser", "new@example.com", "Test1234A");

            Assert.False(success);
            Assert.Equal("Internal Server Error", error);
        }

        // ── LoginAsync ────────────────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task LoginAsync_ReturnsSuccess_AndStoresToken_WhenCredentialsValid()
        {
            var token = BuildJwt("user1");
            var (service, _, authState, _) = CreateService(HttpStatusCode.OK, SuccessBody(token));

            var (success, error) = await service.LoginAsync("user@example.com", "Test1234A");

            Assert.True(success);
            Assert.Null(error);
            Assert.Equal(token, await authState.GetToken());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task LoginAsync_ReturnsInvalidCredentialsMessage_On401()
        {
            // Body intentionally doesn't match any shape ExtractFriendlyError understands --
            // the 401 branch is special-cased and should never reach ExtractFriendlyError.
            var (service, _, _, _) = CreateService(HttpStatusCode.Unauthorized, """{ "something": "unexpected" }""");

            var (success, error) = await service.LoginAsync("user@example.com", "WrongPassword");

            Assert.False(success);
            Assert.Equal("Invalid email or password.", error);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task LoginAsync_ReturnsFriendlyError_ForNon401Failure()
        {
            var (service, _, _, _) = CreateService(HttpStatusCode.InternalServerError,
                """{ "title": "Something went wrong." }""");

            var (success, error) = await service.LoginAsync("user@example.com", "Test1234A");

            Assert.False(success);
            Assert.Equal("Something went wrong.", error);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task LoginAsync_ClearsStaleAuthorizationHeader_BeforeSendingRequest()
        {
            var (service, handler, _, httpClient) = CreateService(HttpStatusCode.OK, SuccessBody());
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "stale-token");

            await service.LoginAsync("user@example.com", "Test1234A");

            // The request actually sent must not carry the stale header, even though a fresh one
            // gets set on the client *after* this response is processed.
            Assert.Null(handler.LastRequest!.Headers.Authorization);
        }

        // ── Logout ────────────────────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Logout_ClearsStoredToken()
        {
            var (service, _, authState, _) = CreateService(HttpStatusCode.OK, SuccessBody(BuildJwt()));
            await service.LoginAsync("user@example.com", "Test1234A");

            await service.Logout();

            Assert.Null(await authState.GetToken());
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Logout_ClearsDefaultAuthorizationHeader_OnHttpClient()
        {
            var (service, _, _, httpClient) = CreateService(HttpStatusCode.OK, SuccessBody(BuildJwt()));
            await service.LoginAsync("user@example.com", "Test1234A");

            await service.Logout();

            Assert.Null(httpClient.DefaultRequestHeaders.Authorization);
        }
    }
}
