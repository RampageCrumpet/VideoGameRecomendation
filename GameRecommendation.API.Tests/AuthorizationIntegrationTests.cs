using GameRecommendation.Infrastructure.Data;
using GameRecommendation.TestUtilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace GameRecommendation.API.Tests
{
    /// <summary>
    /// Verifies that the [Authorize] attribute on protected endpoints is actually enforced by
    /// the authentication pipeline -- i.e. that requests are genuinely rejected, not just that
    /// a valid token is accepted. Controller-level unit tests call actions directly and never
    /// exercise this pipeline, so this gap can only be closed with a real HTTP round trip.
    /// </summary>
    public class AuthorizationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> factory;
        private const string TestJwtKey = "TestKey_32_Characters_Long_For_HmacSha256!";
        private const string OtherJwtKey = "Different_32_Character_Long_Signing_Key!!";
        private const string TestJwtIssuer = "GameRecommendation.API";
        private const string TestJwtAudience = "GameRecommendation.Web";

        public AuthorizationIntegrationTests(WebApplicationFactory<Program> factory)
        {
            this.factory = factory;
        }

        private HttpClient CreateTestClient(string dbName)
        {
            return factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Key"] = TestJwtKey
                    });
                });

                builder.ConfigureServices(services =>
                {
                    var descriptorsToRemove = services
                        .Where(d =>
                            d.ServiceType == typeof(DbContextOptions<RecommendationDbContext>) ||
                            d.ServiceType == typeof(DbContextOptions) ||
                            d.ServiceType == typeof(RecommendationDbContext) ||
                            d.ServiceType == typeof(IRecommendationDbContext) ||
                            (d.ImplementationType != null &&
                             d.ImplementationType.IsAssignableTo(typeof(RecommendationDbContext))))
                        .ToList();

                    foreach (var d in descriptorsToRemove)
                        services.Remove(d);

                    services.AddDbContext<RecommendationDbContext, TestRecommendationDbContext>(options => options.UseInMemoryDatabase(dbName));

                    services.AddScoped<IRecommendationDbContext>(sp =>
                        sp.GetRequiredService<RecommendationDbContext>());

                    services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = TestJwtIssuer,
                            ValidAudience = TestJwtAudience,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(TestJwtKey))
                        };
                    });
                });

                builder.ConfigureServices(services =>
                {
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<RecommendationDbContext>();
                    db.Database.EnsureCreated();
                });
            })
            .CreateClient();
        }

        private static async Task<string> RegisterAndGetTokenAsync(HttpClient client, string userName)
        {
            var response = await client.PostAsJsonAsync("api/auth/register", new
            {
                userName,
                email = $"{userName}@example.com",
                password = "Test1234A"
            }, TestContext.Current.CancellationToken);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
            return body.GetProperty("token").GetString()!;
        }

        /// <summary>Signs a token with the same claims/issuer/audience as a real one, but a key the server doesn't trust.</summary>
        private static string CreateTokenSignedWithWrongKey()
        {
            var claims = new[] { new System.Security.Claims.Claim("sub", "someone") };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(OtherJwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: TestJwtIssuer,
                audience: TestJwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials);

            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }

        public static TheoryData<HttpMethod, string> ProtectedEndpoints => new()
        {
            { HttpMethod.Get, "api/games" },
            { HttpMethod.Get, "api/games/1" },
            { HttpMethod.Get, "api/games/unrated" },
            { HttpMethod.Get, "api/recommendations" },
            { HttpMethod.Put, "api/ratings" },
            { HttpMethod.Delete, "api/ratings/1" },
        };

        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(ProtectedEndpoints))]
        public async Task ProtectedEndpoint_ReturnsUnauthorized_WhenNoTokenProvided(HttpMethod method, string path)
        {
            var client = CreateTestClient($"{nameof(ProtectedEndpoint_ReturnsUnauthorized_WhenNoTokenProvided)}_{method}_{path.Replace('/', '_')}");

            var response = await client.SendAsync(new HttpRequestMessage(method, path), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [Trait("Category", "Integration")]
        [MemberData(nameof(ProtectedEndpoints))]
        public async Task ProtectedEndpoint_ReturnsUnauthorized_WhenTokenIsGarbage(HttpMethod method, string path)
        {
            var client = CreateTestClient($"{nameof(ProtectedEndpoint_ReturnsUnauthorized_WhenTokenIsGarbage)}_{method}_{path.Replace('/', '_')}");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-real-token");

            var response = await client.SendAsync(new HttpRequestMessage(method, path), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ProtectedEndpoint_ReturnsUnauthorized_WhenTokenSignedWithWrongKey()
        {
            var client = CreateTestClient(nameof(ProtectedEndpoint_ReturnsUnauthorized_WhenTokenSignedWithWrongKey));
            var badToken = CreateTokenSignedWithWrongKey();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", badToken);

            var response = await client.GetAsync("api/recommendations", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ProtectedEndpoint_ReturnsOk_WhenTokenIsValid()
        {
            var client = CreateTestClient(nameof(ProtectedEndpoint_ReturnsOk_WhenTokenIsValid));
            var token = await RegisterAndGetTokenAsync(client, "valid_token_user");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/recommendations", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task AuthEndpoints_DoNotRequireAuthorization()
        {
            var client = CreateTestClient(nameof(AuthEndpoints_DoNotRequireAuthorization));

            var response = await client.PostAsJsonAsync("api/auth/login", new
            {
                email = "nobody@example.com",
                password = "WrongPassword1"
            }, TestContext.Current.CancellationToken);

            // A bare 401 doesn't prove [AllowAnonymous] works -- the auth *middleware* would also
            // produce a 401 challenge with no body if this endpoint were blocked before reaching the
            // action. Assert on the action's own error body to prove it actually executed.
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
            Assert.True(body.TryGetProperty("error", out var error));
            Assert.Equal("Invalid email or password.", error.GetString());
        }
    }
}
