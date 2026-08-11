using GameRecommendation.Web.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text;
using System.Text.Json;

namespace GameRecommendation.Web.Tests
{
    public class JwtAuthenticationStateProviderTests
    {
        private readonly FakeJsRuntime js = new();
        private JwtAuthenticationStateProvider CreateProvider() => new(js);

        /// <summary>Builds a syntactically valid (unsigned) JWT with the given claims in its payload.</summary>
        private static string BuildJwt(Dictionary<string, object> claims)
        {
            var payloadJson = JsonSerializer.Serialize(claims);
            var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            return $"header.{payloadBase64}.signature";
        }

        // ── GetAuthenticationStateAsync ──────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAuthenticationStateAsync_ReturnsAnonymous_WhenNoTokenStored()
        {
            var state = await CreateProvider().GetAuthenticationStateAsync();

            Assert.False(state.User.Identity?.IsAuthenticated);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAuthenticationStateAsync_ReturnsAuthenticatedPrincipal_WhenTokenStored()
        {
            js.SeedToken(BuildJwt(new() { ["sub"] = "user1" }));

            var state = await CreateProvider().GetAuthenticationStateAsync();

            Assert.True(state.User.Identity?.IsAuthenticated);
            Assert.Equal("user1", state.User.FindFirst("sub")?.Value);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAuthenticationStateAsync_DecodesMultipleClaims()
        {
            js.SeedToken(BuildJwt(new() { ["sub"] = "user1", ["name"] = "Player One", ["role"] = "Admin" }));

            var state = await CreateProvider().GetAuthenticationStateAsync();

            Assert.Equal("user1", state.User.FindFirst("sub")?.Value);
            Assert.Equal("Player One", state.User.FindFirst("name")?.Value);
            Assert.Equal("Admin", state.User.FindFirst("role")?.Value);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAuthenticationStateAsync_CoercesNumericClaim_ToItsRawTextForm()
        {
            // Real JWTs commonly carry numeric claims (e.g. "exp"). ParseClaimsFromJwt converts
            // every claim value via JsonElement.ToString() regardless of its JSON kind -- pinning
            // down exactly what that produces for a number.
            js.SeedToken(BuildJwt(new() { ["exp"] = 1735689600 }));

            var state = await CreateProvider().GetAuthenticationStateAsync();

            Assert.Equal("1735689600", state.User.FindFirst("exp")?.Value);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAuthenticationStateAsync_CoercesBooleanClaim_ToPascalCase()
        {
            // JsonElement.ToString() on a JSON `true`/`false` renders "True"/"False" (.NET's
            // bool.ToString() convention), NOT the lowercase JSON spelling. Worth knowing if any
            // future code does a literal `== "true"` comparison against a boolean claim -- it
            // would never match.
            js.SeedToken(BuildJwt(new() { ["isAdmin"] = true }));

            var state = await CreateProvider().GetAuthenticationStateAsync();

            Assert.Equal("True", state.User.FindFirst("isAdmin")?.Value);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAuthenticationStateAsync_Throws_WhenStoredTokenHasNoPayloadSegment()
        {
            // Documents current behavior: unlike GetToken(), this path has no try/catch, so a
            // corrupted localStorage value (e.g. from a bad manual edit) surfaces as an unhandled
            // exception rather than falling back to Anonymous.
            js.SeedToken("not-a-real-jwt");

            await Assert.ThrowsAsync<IndexOutOfRangeException>(() => CreateProvider().GetAuthenticationStateAsync());
        }

        // ── NotifyUserAuthenticated ──────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task NotifyUserAuthenticated_PersistsTokenToLocalStorage()
        {
            var token = BuildJwt(new() { ["sub"] = "user1" });

            await CreateProvider().NotifyUserAuthenticated(token);

            Assert.Contains(js.Invocations, i => i.Identifier == "localStorage.setItem" && (string?)i.Args![1] == token);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task NotifyUserAuthenticated_RaisesAuthenticationStateChanged_WithAuthenticatedPrincipal()
        {
            var provider = CreateProvider();
            Task<AuthenticationState>? raised = null;
            provider.AuthenticationStateChanged += task => raised = task;

            await provider.NotifyUserAuthenticated(BuildJwt(new() { ["sub"] = "user1" }));

            Assert.NotNull(raised);
            var state = await raised!;
            Assert.True(state.User.Identity?.IsAuthenticated);
        }

        // ── NotifyUserLoggedOut ───────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task NotifyUserLoggedOut_RemovesTokenFromLocalStorage()
        {
            var provider = CreateProvider();
            await provider.NotifyUserAuthenticated(BuildJwt(new() { ["sub"] = "user1" }));

            await provider.NotifyUserLoggedOut();

            Assert.Contains(js.Invocations, i => i.Identifier == "localStorage.removeItem");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task NotifyUserLoggedOut_RaisesAuthenticationStateChanged_WithAnonymousPrincipal()
        {
            var provider = CreateProvider();
            Task<AuthenticationState>? raised = null;
            provider.AuthenticationStateChanged += task => raised = task;

            await provider.NotifyUserLoggedOut();

            Assert.NotNull(raised);
            var state = await raised!;
            Assert.False(state.User.Identity?.IsAuthenticated);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task NotifyUserLoggedOut_ClearsCachedToken_SoGetTokenReturnsNull()
        {
            var provider = CreateProvider();
            await provider.NotifyUserAuthenticated(BuildJwt(new() { ["sub"] = "user1" }));

            await provider.NotifyUserLoggedOut();
            var token = await provider.GetToken();

            Assert.Null(token);
        }

        // ── GetToken ──────────────────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetToken_ReturnsStoredToken_WhenNotYetCached()
        {
            js.SeedToken("abc.def.ghi");

            var token = await CreateProvider().GetToken();

            Assert.Equal("abc.def.ghi", token);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetToken_ReturnsNull_WhenNoTokenStored()
        {
            var token = await CreateProvider().GetToken();

            Assert.Null(token);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetToken_UsesCachedToken_AfterNotifyUserAuthenticated_WithoutHittingJsAgain()
        {
            var provider = CreateProvider();
            await provider.NotifyUserAuthenticated(BuildJwt(new() { ["sub"] = "user1" }));
            var invocationsAfterAuth = js.Invocations.Count;

            var token = await provider.GetToken();

            Assert.NotNull(token);
            Assert.Equal(invocationsAfterAuth, js.Invocations.Count); // no additional JS interop call
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetToken_ReturnsNull_WhenJsInteropThrows()
        {
            js.ThrowOnInvoke = new InvalidOperationException("JS interop unavailable (e.g. prerendering)");

            var token = await CreateProvider().GetToken();

            Assert.Null(token);
        }
    }
}
