using GameRecommendation.Domain.Models.Domain;
using System.Text.Json;

namespace GameRecommendation.SteamImporter.Services
{
    public class SteamGameMapper : ISteamGameMapper
    {
        public Game? Map(int appId, JsonDocument json)
        {
            if (!json.RootElement.TryGetProperty(appId.ToString(), out var root))
                return null;

            if (!root.TryGetProperty("success", out var successProp) ||
                !successProp.GetBoolean())
                return null;

            if (!root.TryGetProperty("data", out var data))
                return null;

            return new Game
            {
                SteamAppId = appId,
                Name = data.GetProperty("name").GetString() ?? "",
                Description = data.GetProperty("short_description").GetString() ?? "",
                ImageUrl = data.GetProperty("header_image").GetString() ?? ""
            };
        }

        private static DateTime ParseReleaseDate(JsonElement data)
        {
            if (!data.TryGetProperty("release_date", out var rd))
                return DateTime.MinValue;

            if (rd.TryGetProperty("date", out var dateStr) &&
                DateTime.TryParse(dateStr.GetString(), out var parsed))
            {
                return parsed;
            }

            return DateTime.MinValue;
        }
    }
}
