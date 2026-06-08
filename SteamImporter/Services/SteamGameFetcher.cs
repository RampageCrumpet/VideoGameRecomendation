using System.Text.Json;

namespace GameRecommendation.SteamImporter.Services
{
    public class SteamGameFetcher : ISteamGameFetcher
    {
        private readonly HttpClient httpClient;

        public SteamGameFetcher(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<JsonDocument?> GetGameAsync(int appId)
        {
            var url = $"https://store.steampowered.com/api/appdetails?appids={appId}";

            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch game with appId {appId}: {ex.Message}");
                return null;
            }
        }
    }
}
