using GameRecommendation.Domain.Models.Domain;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameRecommendation.SteamImporter.Services
{
    /// <summary>
    /// Maps raw Steam API JSON responses to <see cref="Game"/> domain models.
    /// </summary>
    public class SteamGameMapper : ISteamGameMapper
    {
        private readonly ILogger<SteamGameMapper> logger;

        public SteamGameMapper(ILogger<SteamGameMapper> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Maps the raw Steam API JSON response for the given AppId to a <see cref="Game"/> domain model.
        /// </summary>
        /// <param name="appId">The Steam AppId of the game being mapped.</param>
        /// <param name="json">The raw <see cref="JsonDocument"/> returned by the Steam store API.</param>
        /// <returns>A <see cref="Game"/> domain model, or null if the response was invalid or unsuccessful.</returns>
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

        /// <summary>
        /// Attempts to parse the release date from the given <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="appId">The Steam AppId of the game being parsed, used for logging.</param>
        /// <param name="releaseDate">The <see cref="JsonElement"/> containing the release date data.</param>
        /// <returns>The parsed <see cref="DateTime"/>, or <see cref="DateTime.MinValue"/> if parsing failed.</returns>
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
