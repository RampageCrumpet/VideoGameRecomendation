using GameRecommendation.Domain.Enums;
using GameRecommendation.Domain.Models;
using GameRecommendation.Web.Auth;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace GameRecommendation.Web.Services
{
    /// <summary>
    /// Handles communication with the games API endpoints.
    /// </summary>
    public class GamesService
    {
        private readonly HttpClient httpClient;
        private readonly JwtAuthenticationStateProvider authStateProvider;

        public GamesService(HttpClient httpClient, JwtAuthenticationStateProvider authStateProvider)
        {
            this.httpClient = httpClient;
            this.authStateProvider = authStateProvider;
        }

        /// <summary>
        /// Returns a paginated, searchable list of games.
        /// </summary>
        public async Task<PagedResult<GameSummaryResponse>?> GetGamesAsync(int page = 1, int pageSize = 20, string? search = null)
        {
            AttachToken();
            var url = $"api/games?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";

            return await httpClient.GetFromJsonAsync<PagedResult<GameSummaryResponse>>(url);
        }

        /// <summary>
        /// Returns the full details for a single game.
        /// </summary>
        public async Task<GameDetailResponse?> GetGameAsync(int id)
        {
            AttachToken();
            return await httpClient.GetFromJsonAsync<GameDetailResponse>($"api/games/{id}");
        }

        private void AttachToken()
        {
            var token = authStateProvider.GetToken();
            if (token != null)
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
        }

        public class GameSummaryResponse
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
            public DateTime ReleaseDate { get; set; }
            public RatingType? UserRating { get; set; }
        }

        public class GameDetailResponse
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
            public DateTime ReleaseDate { get; set; }
            public IEnumerable<string> Tags { get; set; } = [];
            public RatingType? UserRating { get; set; }
        }
    }
}