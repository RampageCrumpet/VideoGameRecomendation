using GameRecommendation.Web.Auth;
using System.Net.Http.Headers;
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

        private class IdentityErrorResponse
        {
            public string Code { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        /// <summary>
        /// Registers a new user account and stores the returned JWT on success.
        /// </summary>
        public async Task<(bool success, string? error)> RegisterAsync(string userName, string email, string password)
        {
            httpClient.DefaultRequestHeaders.Authorization = null;
            var response = await httpClient.PostAsJsonAsync("api/auth/register", new
            {
                userName,
                email,
                password
            });

            if (!response.IsSuccessStatusCode)
            {
                var errors = await response.Content.ReadFromJsonAsync<List<IdentityErrorResponse>>();
                var message = errors != null
                    ? string.Join(" ", errors.Select(e => e.Description))
                    : "Registration failed.";
                return (false, message);
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            authStateProvider.NotifyUserAuthenticated(result!.Token);

            return (true, null);
        }

        /// <summary>
        /// Logs in to an existing account and stores the returned JWT on success.
        /// </summary>
        public async Task<(bool success, string? error)> LoginAsync(string email, string password)
        {
            httpClient.DefaultRequestHeaders.Authorization = null;
            var response = await httpClient.PostAsJsonAsync("api/auth/login", new
            {
                email,
                password
            });

            if (!response.IsSuccessStatusCode)
            {
                var errors = await response.Content.ReadFromJsonAsync<List<IdentityErrorResponse>>();
                var message = errors != null
                    ? string.Join(" ", errors.Select(e => e.Description))
                    : "Login failed.";
                return (false, message);
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            authStateProvider.NotifyUserAuthenticated(result!.Token);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);

            return (true, null);
        }

        /// <summary>
        /// Logs out the current user by clearing the stored JWT.
        /// </summary>
        public void Logout()
        {
            httpClient.DefaultRequestHeaders.Authorization = null;
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