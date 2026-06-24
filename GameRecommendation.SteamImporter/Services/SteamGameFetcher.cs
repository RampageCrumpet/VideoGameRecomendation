using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameRecommendation.SteamImporter.Services
{
    /// <summary>
    /// Fetches game data from the Steam store API.
    /// </summary>
    public class SteamGameFetcher : ISteamGameFetcher
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<SteamGameFetcher> logger;

        public SteamGameFetcher(HttpClient httpClient, ILogger<SteamGameFetcher> logger)
        {
            this.httpClient = httpClient;
            this.logger = logger;
        }

        /// <summary>
        /// Fetches the raw game data for the given Steam AppId from the Steam store API.
        /// </summary>
        /// <param name="appId">The Steam AppId of the game to fetch.</param>
        /// <returns>A <see cref="JsonDocument"/> containing the raw game data, or null if the request failed.</returns>
        public async Task<JsonDocument?> GetGameAsync(int appId)
        {
            var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&l=english";

            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "HTTP error fetching appId {AppId}", appId);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error fetching appId {AppId}", appId);
                return null;
            }
        }
    }
}
