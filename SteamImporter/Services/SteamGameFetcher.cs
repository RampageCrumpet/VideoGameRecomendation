using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace GameRecomendation.SteamImporter.Services
{
    public class SteamGameFetcher
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
                var stream = await httpClient.GetStreamAsync(url);

                return await JsonDocument.ParseAsync(stream);
            }
            catch
            {
                Console.WriteLine($"Failed to fetch game with appId {appId}");
                return null;
            }
        }
    }
}
