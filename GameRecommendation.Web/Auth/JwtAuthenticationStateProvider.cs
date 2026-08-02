using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;

namespace GameRecommendation.Web.Auth
{
    /// <summary>
    /// Manages authentication state for the Blazor application using a JWT persisted to localStorage.
    /// </summary>
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly AuthenticationState Anonymous =
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        private readonly IJSRuntime js;
        private const string TokenKey = "authToken";
        private string? _cachedToken;

        public JwtAuthenticationStateProvider(IJSRuntime js)
        {
            this.js = js;
        }

        /// <inheritdoc/>
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await js.InvokeAsync<string?>("localStorage.getItem", TokenKey);

            if (string.IsNullOrWhiteSpace(token))
                return Anonymous;

            var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        /// <summary>
        /// Stores the JWT in localStorage and notifies the application that authentication state has changed.
        /// </summary>
        public async Task NotifyUserAuthenticated(string jwtToken)
        {
            _cachedToken = jwtToken;
            await js.InvokeVoidAsync("localStorage.setItem", TokenKey, jwtToken);
            var identity = new ClaimsIdentity(ParseClaimsFromJwt(jwtToken), "jwt");
            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));

        }

        /// <summary>
        /// Removes the JWT from localStorage and notifies the application that the user has logged out.
        /// </summary>
        public async Task NotifyUserLoggedOut()
        {
            _cachedToken = null;
            await js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
        }

        /// <summary>
        /// Returns the stored JWT from localStorage for attaching to outgoing HTTP requests, or null if not authenticated.
        /// </summary>
        public async Task<string?> GetToken()
        {
            if (_cachedToken != null)
                return _cachedToken;

            try
            {
                var token = await js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
                if (token != null)
                    _cachedToken = token;
                return token;
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwtToken)
        {
            var payload = jwtToken.Split('.')[1];

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