using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace GameRecommendation.SteamImporter.Data
{
    public class SteamAppListSource : IAppIdSource
    {
        private readonly HttpClient httpClient;

        public SteamAppListSource(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<IEnumerable<int>> GetAppIdsAsync()
        {
            var response = await httpClient.GetAsync(
                "https://api.steampowered.com/ISteamApps/GetAppList/v2/");

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();

            using var document = await JsonDocument.ParseAsync(stream);

            var apps = document.RootElement
                .GetProperty("applist")
                .GetProperty("apps");

            return apps
                .EnumerateArray()
                .Select(x => x.GetProperty("appid").GetInt32())
                .ToList();
        }
    }
}
