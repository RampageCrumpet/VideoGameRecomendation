using GameRecommendation.Infrastructure.Data;
using GameRecommendation.TestUtilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace GameRecommendation.API.Tests
{
    public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> factory;
        private const string TestJwtKey = "TestKey_32_Characters_Long_For_HmacSha256!";
        private const string TestJwtIssuer = "GameRecommendation.API";
        private const string TestJwtAudience = "GameRecommendation.Web";

        public AuthIntegrationTests(WebApplicationFactory<Program> factory)
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

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Register_Login_Then_AccessProtectedEndpoint_ReturnsTokenAndOk()
        {
            var client = CreateTestClient(nameof(Register_Login_Then_AccessProtectedEndpoint_ReturnsTokenAndOk));

            var registerPayload = new
            {
                userName = "integ_test_user",
                email = "integ_test_user@example.com",
                password = "Test1234A"
            };

            // Register
            var registerResponse = await client.PostAsJsonAsync("api/auth/register", registerPayload);
            Assert.Equal(System.Net.HttpStatusCode.Created, registerResponse.StatusCode);

            var registerBody = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(registerBody.TryGetProperty("token", out var tokenElement));
            var token = tokenElement.GetString();
            Assert.False(string.IsNullOrWhiteSpace(token));

            // Logout client-level headers if any and then login
            var loginPayload = new
            {
                email = registerPayload.email,
                password = registerPayload.password
            };

            var loginResponse = await client.PostAsJsonAsync("api/auth/login", loginPayload);
            Assert.Equal(System.Net.HttpStatusCode.OK, loginResponse.StatusCode);

            var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(loginBody.TryGetProperty("token", out var loginTokenElement));
            var loginToken = loginTokenElement.GetString();
            Assert.False(string.IsNullOrWhiteSpace(loginToken));

            // Use token to call protected endpoint
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginToken);

            var protectedResponse = await client.GetAsync("api/recommendations");

            Assert.NotEqual(System.Net.HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
        }
    }
}