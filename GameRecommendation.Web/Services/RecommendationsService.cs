using GameRecommendation.Domain.Models;
using GameRecommendation.Web.Auth;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace GameRecommendation.Web.Services
{
    /// <summary>
    /// Handles communication with the recommendations API endpoints.
    /// </summary>
    public class RecommendationsService
    {
        private readonly HttpClient httpClient;
        private readonly JwtAuthenticationStateProvider authStateProvider;

        public RecommendationsService(HttpClient httpClient, JwtAuthenticationStateProvider authStateProvider)
        {
            this.httpClient = httpClient;
            this.authStateProvider = authStateProvider;
        }

        /// <summary>
        /// Returns a paginated list of game recommendations for the current user.
        /// </summary>
        public async Task<PagedResult<RecommendationResponse>?> GetRecommendationsAsync(int page = 1, int pageSize = 20)
        {
            AttachToken();
            return await httpClient.GetFromJsonAsync<PagedResult<RecommendationResponse>>(
                $"api/recommendations?page={page}&pageSize={pageSize}");
        }

        private void AttachToken()
        {
            var token = authStateProvider.GetToken();
            if (token != null)
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
        }

        public class RecommendationResponse
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
            public DateTime ReleaseDate { get; set; }
            public double Score { get; set; }
        }
    }
}