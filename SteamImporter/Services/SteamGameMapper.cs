using GameRecommendation.Domain.Models.Domain;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameRecommendation.SteamImporter.Services
{
    public class SteamGameMapper : ISteamGameMapper
    {
        private readonly ILogger<SteamGameMapper> logger;

        public SteamGameMapper(ILogger<SteamGameMapper> logger)
        {
            this.logger = logger;
        }

        public Game? Map(int appId, JsonDocument json)
        {
            if (!json.RootElement.TryGetProperty(appId.ToString(), out var root))
            {
                logger.LogWarning("AppId {AppId} not found in JSON response", appId);
                return null;
            }

            if (!root.TryGetProperty("success", out var successProp) ||
                !successProp.GetBoolean())
            {
                logger.LogWarning("AppId {AppId} returned success=false", appId);
                return null;
            }

            if (!root.TryGetProperty("data", out var data))
            {
                logger.LogWarning("AppId {AppId} has no data property", appId);
                return null;
            }

            return new Game
            {
                SteamAppId = appId,
                Name = data.GetProperty("name").GetString() ?? "",
                Description = data.GetProperty("short_description").GetString() ?? "",
                ImageUrl = data.GetProperty("header_image").GetString() ?? "",
                ReleaseDate = ParseReleaseDate(appId, data.GetProperty("release_date"))
            };
        }

        private DateTime ParseReleaseDate(int appId, JsonElement releaseDate)
        {
            if (releaseDate.TryGetProperty("date", out var dateStr) &&
                DateTime.TryParse(dateStr.GetString(), out var parsed))
            {
                return parsed;
            }

            logger.LogWarning("AppId {AppId} has unparseable release date: '{Date}'",
                appId, releaseDate.TryGetProperty("date", out var raw) ? raw.GetString() : "missing");

            return DateTime.MinValue;
        }
    }
}
