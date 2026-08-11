using GameRecommendation.API.Controllers;
using GameRecommendation.API.DataTransferObjects.Common;
using GameRecommendation.API.DataTransferObjects.Recommendations;
using GameRecommendation.API.Services;
using GameRecommendation.Domain.Models;
using GameRecommendation.Domain.Models.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace GameRecommendation.API.Tests
{
    public class RecommendationsControllerTests
    {
        private readonly Mock<IRecommendationService> service = new();
        private readonly RecommendationsController controller;

        public RecommendationsControllerTests()
        {
            controller = new RecommendationsController(service.Object);

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user1") }, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        private static Game MakeGame(int id, string name) => new()
        {
            Id = id,
            SteamAppId = id,
            Name = name,
            Description = "Description",
            ImageUrl = $"https://image/{id}.jpg",
            ReleaseDate = new DateTime(2023, 1, 1)
        };

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendations_ReturnsMappedItems()
        {
            service.Setup(s => s.GetRecommendationsAsync("user1", 1, 20))
                .ReturnsAsync(new PagedResult<RecommendationResult>
                {
                    Items = new[]
                    {
                        new RecommendationResult { Game = MakeGame(1, "Game 1"), Score = 0.9 }
                    },
                    TotalCount = 1,
                    PageSize = 20
                });

            var result = await controller.GetRecommendations();

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<PagedResultDto<RecommendationDto>>(ok.Value);
            var dto = Assert.Single(payload.Items);
            Assert.Equal(1, dto.Id);
            Assert.Equal("Game 1", dto.Name);
            Assert.Equal(0.9, dto.Score);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendations_PassesCallingUserIdAndPagingToService()
        {
            service.Setup(s => s.GetRecommendationsAsync("user1", 2, 5))
                .ReturnsAsync(new PagedResult<RecommendationResult> { Items = [], TotalCount = 0, PageSize = 5 });

            await controller.GetRecommendations(page: 2, pageSize: 5);

            service.Verify(s => s.GetRecommendationsAsync("user1", 2, 5), Times.Once);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendations_ReturnsEmpty_WhenNoRecommendations()
        {
            service.Setup(s => s.GetRecommendationsAsync("user1", 1, 20))
                .ReturnsAsync(new PagedResult<RecommendationResult> { Items = [], TotalCount = 0, PageSize = 20 });

            var result = await controller.GetRecommendations();

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<PagedResultDto<RecommendationDto>>(ok.Value);
            Assert.Empty(payload.Items);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendations_ReflectsPageAndPageSizeInResponse()
        {
            service.Setup(s => s.GetRecommendationsAsync("user1", 3, 10))
                .ReturnsAsync(new PagedResult<RecommendationResult> { Items = [], TotalCount = 25, PageSize = 10 });

            var result = await controller.GetRecommendations(page: 3, pageSize: 10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<PagedResultDto<RecommendationDto>>(ok.Value);
            Assert.Equal(3, payload.Page);
            Assert.Equal(10, payload.PageSize);
            Assert.Equal(25, payload.TotalCount);
        }

        [Theory]
        [Trait("Category", "Unit")]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetRecommendations_ReturnsBadRequest_WhenPageIsLessThanOne(int page)
        {
            var result = await controller.GetRecommendations(page: page);

            Assert.IsType<BadRequestObjectResult>(result);
            service.Verify(s => s.GetRecommendationsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [Trait("Category", "Unit")]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetRecommendations_ReturnsBadRequest_WhenPageSizeIsLessThanOne(int pageSize)
        {
            var result = await controller.GetRecommendations(pageSize: pageSize);

            Assert.IsType<BadRequestObjectResult>(result);
            service.Verify(s => s.GetRecommendationsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetRecommendations_PreservesResultOrdering()
        {
            service.Setup(s => s.GetRecommendationsAsync("user1", 1, 20))
                .ReturnsAsync(new PagedResult<RecommendationResult>
                {
                    Items = new[]
                    {
                        new RecommendationResult { Game = MakeGame(1, "Best Match"), Score = 0.95 },
                        new RecommendationResult { Game = MakeGame(2, "Worse Match"), Score = 0.2 }
                    },
                    TotalCount = 2,
                    PageSize = 20
                });

            var result = await controller.GetRecommendations();

            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<PagedResultDto<RecommendationDto>>(ok.Value);
            var items = payload.Items.ToList();
            Assert.Equal("Best Match", items[0].Name);
            Assert.Equal("Worse Match", items[1].Name);
        }
    }
}
