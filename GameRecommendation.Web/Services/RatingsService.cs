using GameRecommendation.Domain.Enums;
using GameRecommendation.Web.Auth;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace GameRecommendation.Web.Services
{
    /// <summary>
    /// Handles communication with the ratings API endpoints.
    /// </summary>
    public class RatingsService
    {
        private readonly HttpClient httpClient;
        private readonly JwtAuthenticationStateProvider authStateProvider;

        public RatingsService(HttpClient httpClient, JwtAuthenticationStateProvider authStateProvider)
        {
            this.httpClient = httpClient;
            this.authStateProvider = authStateProvider;
        }

        /// <summary>
        /// Creates or updates the current user's rating for a game.
        /// </summary>
        public async Task<bool> UpsertRatingAsync(int gameId, RatingType rating)
        {
            AttachToken();
            var response = await httpClient.PutAsJsonAsync("api/ratings", new { gameId, rating });
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Deletes the current user's rating for a game.
        /// </summary>
        public async Task<bool> DeleteRatingAsync(int gameId)
        {
            AttachToken();
            var response = await httpClient.DeleteAsync($"api/ratings/{gameId}");
            return response.IsSuccessStatusCode;
        }

        private void AttachToken()
        {
            var token = authStateProvider.GetToken();
            if (token != null)
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
        }
    }
}