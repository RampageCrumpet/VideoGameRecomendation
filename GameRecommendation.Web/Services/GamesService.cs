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
        public async Task<PagedResult<GameSummaryResponse>?> GetGamesAsync(int page = 1, int pageSize = 20, string? search = null, bool ratedOnly = true)
        {
            await AttachTokenAsync();
            var url = $"api/games?page={page}&pageSize={pageSize}&ratedOnly={ratedOnly.ToString().ToLower()}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search)}";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<PagedResult<GameSummaryResponse>>();
        }

        /// <summary>
        /// Returns a single unrated game for the current user, or null when none available.
        /// </summary>
        public async Task<GameDetailResponse?> GetUnratedGameAsync()
        {
            await AttachTokenAsync();
            var response = await httpClient.GetAsync("api/games/unrated");
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return null;

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<GameDetailResponse>();

            return null;
        }

        /// <summary>
        /// Returns the full details for a single game.
        /// </summary>
        public async Task<GameDetailResponse?> GetGameAsync(int id)
        {
            await AttachTokenAsync();
            return await httpClient.GetFromJsonAsync<GameDetailResponse>($"api/games/{id}");
        }

        private async Task AttachTokenAsync()
        {
            var token = await authStateProvider.GetToken();
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
            public IEnumerable<string> Tags { get; set; } = new List<string>();
            public RatingType? UserRating { get; set; }
        }
    }
}