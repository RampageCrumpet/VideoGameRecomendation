using GameRecommendation.Web.Auth;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;

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
        /// Attempts to extract the user friendly string from an error message.
        /// </summary>
        private static string ExtractFriendlyError(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "An error occurred.";

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in errors.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Array && prop.Value.GetArrayLength() > 0)
                                return prop.Value[0].GetString() ?? "Validation error.";
                        }
                    }

                    if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                        return title.GetString()!;

                    if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                        return detail.GetString()!;
                }

                if (root.ValueKind == JsonValueKind.Array)
                {
                    var descriptions = new List<string>();
                    foreach (var item in root.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object)
                            continue;

                        if (item.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String)
                        {
                            var s = desc.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                                descriptions.Add(s.Trim());
                        }
                        else if (item.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String)
                        {
                            var s = code.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                                descriptions.Add(s.Trim());
                        }
                    }

                    if (descriptions.Count > 0)
                        return string.Join(" ", descriptions);
                }
            }
            catch
            {
                // ignore parse errors
            }

            return content.Length > 0 ? content : "An error occurred.";
        }

        /// <summary>
        /// Registers a new user account and stores the returned JWT on success.
        /// </summary>
        public async Task<(bool success, string? error)> RegisterAsync(string userName, string email, string password)
        {
            httpClient.DefaultRequestHeaders.Authorization = null;
            var response = await httpClient.PostAsJsonAsync("api/auth/register", new { userName, email, password });

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return (false, ExtractFriendlyError(content));
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            await authStateProvider.NotifyUserAuthenticated(result!.Token);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);

            return (true, null);
        }

        /// <summary>
        /// Logs in to an existing account and stores the returned JWT on success.
        /// </summary>
        public async Task<(bool success, string? error)> LoginAsync(string email, string password)
        {
            httpClient.DefaultRequestHeaders.Authorization = null;
            var response = await httpClient.PostAsJsonAsync("api/auth/login", new { email, password });

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return (false, "Invalid email or password.");

                var content = await response.Content.ReadAsStringAsync();
                return (false, ExtractFriendlyError(content));
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            await authStateProvider.NotifyUserAuthenticated(result!.Token);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);

            return (true, null);
        }

        /// <summary>
        /// Logs out the current user by clearing the stored JWT.
        /// </summary>
        public async Task Logout()
        {
            await authStateProvider.NotifyUserLoggedOut();
            httpClient.DefaultRequestHeaders.Authorization = null;
        }

        private class AuthResponse
        {
            public string Token { get; set; } = string.Empty;
            public DateTime ExpiresUtc { get; set; }
            public string UserName { get; set; } = string.Empty;
        }
    }
}