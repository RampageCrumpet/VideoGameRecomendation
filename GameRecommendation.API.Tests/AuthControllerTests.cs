using GameRecommendation.API.Controllers;
using GameRecommendation.API.DataTransferObjects.Auth;
using GameRecommendation.Infrastructure.Data;
using GameRecommendation.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameRecommendation.API.Tests
{
    public class AuthControllerTests
    {
        private const string TestJwtKey = "TestKey_32_Characters_Long_For_HmacSha256!";

        private readonly Mock<UserManager<ApplicationUser>> userManager;
        private readonly TestRecommendationDbContext dbContext;
        private readonly FakeLogger<AuthController> logger = new();
        private readonly AuthController controller;

        public AuthControllerTests()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            userManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var options = new DbContextOptionsBuilder<TestRecommendationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            dbContext = new TestRecommendationDbContext(options);

            var configuration = BuildConfiguration(TestJwtKey);

            controller = new AuthController(userManager.Object, dbContext, configuration, logger);
        }

        private static IConfiguration BuildConfiguration(string? jwtKey) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(jwtKey is null
                    ? new Dictionary<string, string?>()
                    : new Dictionary<string, string?>
                    {
                        ["Jwt:Key"] = jwtKey,
                        ["Jwt:Issuer"] = "GameRecommendation.API",
                        ["Jwt:Audience"] = "GameRecommendation.Web"
                    })
                .Build();

        private static RegisterRequestDto MakeRegisterRequest() => new()
        {
            UserName = "newuser",
            Email = "newuser@example.com",
            Password = "Test1234A"
        };

        // ── Register ──────────────────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Register_ReturnsCreated_WhenRegistrationSucceeds()
        {
            userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            var result = await controller.Register(MakeRegisterRequest());

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var body = Assert.IsType<AuthResponseDto>(created.Value);
            Assert.Equal("newuser", body.UserName);
            Assert.False(string.IsNullOrWhiteSpace(body.Token));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Register_PersistsDomainUser_WhenRegistrationSucceeds()
        {
            userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            await controller.Register(MakeRegisterRequest());

            var domainUser = Assert.Single(dbContext.RatingUsers);
            Assert.Equal("newuser", domainUser.UserName);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Register_ReturnsBadRequest_WhenIdentityCreationFails()
        {
            userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

            var result = await controller.Register(MakeRegisterRequest());

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(dbContext.RatingUsers);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Register_Throws_WhenJwtKeyNotConfigured()
        {
            userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            var controllerWithoutKey = new AuthController(userManager.Object, dbContext, BuildConfiguration(null), logger);

            await Assert.ThrowsAsync<InvalidOperationException>(() => controllerWithoutKey.Register(MakeRegisterRequest()));
        }

        // ── Login ─────────────────────────────────────────────────────────────

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Login_ReturnsOk_WithValidCredentials()
        {
            var user = new ApplicationUser { Id = "user1", UserName = "existinguser", Email = "existing@example.com" };
            userManager.Setup(m => m.FindByEmailAsync("existing@example.com")).ReturnsAsync(user);
            userManager.Setup(m => m.CheckPasswordAsync(user, "Test1234A")).ReturnsAsync(true);

            var result = await controller.Login(new LoginRequestDto { Email = "existing@example.com", Password = "Test1234A" });

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<AuthResponseDto>(ok.Value);
            Assert.Equal("existinguser", body.UserName);
            Assert.False(string.IsNullOrWhiteSpace(body.Token));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
        {
            userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var result = await controller.Login(new LoginRequestDto { Email = "missing@example.com", Password = "Test1234A" });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Login_ReturnsUnauthorized_WhenPasswordIsIncorrect()
        {
            var user = new ApplicationUser { Id = "user1", UserName = "existinguser", Email = "existing@example.com" };
            userManager.Setup(m => m.FindByEmailAsync("existing@example.com")).ReturnsAsync(user);
            userManager.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);

            var result = await controller.Login(new LoginRequestDto { Email = "existing@example.com", Password = "WrongPassword" });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Login_ReturnsProblem_WhenMultipleUsersShareEmail()
        {
            userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Sequence contains more than one element"));

            var result = await controller.Login(new LoginRequestDto { Email = "dup@example.com", Password = "Test1234A" });

            var problem = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Login_LogsError_WhenMultipleUsersShareEmail()
        {
            userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Sequence contains more than one element"));

            await controller.Login(new LoginRequestDto { Email = "dup@example.com", Password = "Test1234A" });

            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
        }
    }
}
