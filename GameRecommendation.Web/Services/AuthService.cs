using GameRecommendation.Web.Auth;
using System.Net.Http.Json;

namespace GameRecommendation.Web.Services
{
    /// <summary>
    /// Handles communication with the authentication API endpoints.
    /// </summary>
    public class AuthService
    {
        private readonly HttpClient httpClient;
        private readonly JwtAuthenticationStateProvider authStateProvider;

        public AuthService(HttpClient httpClient, JwtAuthenticationStateProvider authStateProvider)
        {
            this.httpClient = httpClient;
            this.authStateProvider = authStateProvider;
        }

        /// <summary>
        /// Registers a new user account and stores the returned JWT on success.
        /// </summary>
        public async Task<(bool success, string? error)> RegisterAsync(string userName, string email, string password)
        {
            var response = await httpClient.PostAsJsonAsync("api/auth/register", new
            {
                userName,
                email,
                password
            });

            if (!response.IsSuccessStatusCode)
                return (false, await response.Content.ReadAsStringAsync());

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            authStateProvider.NotifyUserAuthenticated(result!.Token);

            return (true, null);
        }

        /// <summary>
        /// Logs in to an existing account and stores the returned JWT on success.
        /// </summary>
        public async Task<(bool success, string? error)> LoginAsync(string email, string password)
        {
            var response = await httpClient.PostAsJsonAsync("api/auth/login", new
            {
                email,
                password
            });

            if (!response.IsSuccessStatusCode)
                return (false, "Invalid email or password.");

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            authStateProvider.NotifyUserAuthenticated(result!.Token);

            return (true, null);
        }

        /// <summary>
        /// Logs out the current user by clearing the stored JWT.
        /// </summary>
        public void Logout()
        {
            authStateProvider.NotifyUserLoggedOut();
        }

        private class AuthResponse
        {
            public string Token { get; set; } = string.Empty;
            public DateTime ExpiresUtc { get; set; }
            public string UserName { get; set; } = string.Empty;
        }
    }
}