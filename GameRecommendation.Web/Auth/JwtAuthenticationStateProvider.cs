using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace GameRecommendation.Web.Auth
{
    /// <summary>
    /// Manages authentication state for the Blazor application using a JWT stored in memory.
    /// </summary>
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly AuthenticationState Anonymous =
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        private string? token;

        /// <inheritdoc/>
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (string.IsNullOrWhiteSpace(token))
                return Task.FromResult(Anonymous);

            var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }

        /// <summary>
        /// Stores the JWT and notifies the application that authentication state has changed.
        /// </summary>
        public void NotifyUserAuthenticated(string jwtToken)
        {
            token = jwtToken;
            var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));
        }

        /// <summary>
        /// Clears the stored JWT and notifies the application that the user has logged out.
        /// </summary>
        public void NotifyUserLoggedOut()
        {
            token = null;
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
        }

        /// <summary>
        /// Returns the stored JWT for attaching to outgoing HTTP requests, or null if not authenticated.
        /// </summary>
        public string? GetToken() => token;

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwtToken)
        {
            var payload = jwtToken.Split('.')[1];

            // JWT base64url padding
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var jsonBytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes)!;

            return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!));
        }
    }
}