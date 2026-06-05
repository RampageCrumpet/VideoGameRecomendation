using GameRecomendation.Domain.Models.Domain;
using System.Text.Json;

namespace GameRecomendation.SteamImporter.Services
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
    }
}
