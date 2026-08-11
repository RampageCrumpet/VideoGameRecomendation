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
                Name = GetOptionalString(data, "name"),
                Description = GetOptionalString(data, "short_description"),
                ImageUrl = GetOptionalString(data, "header_image"),
                ReleaseDate = ParseReleaseDate(appId, data)
            };
        }

        /// <summary>
        /// Reads a string property from the given <see cref="JsonElement"/>, tolerating a missing
        /// property or a JSON null value by returning an empty string instead of throwing.
        /// </summary>
        /// <param name="data">The <see cref="JsonElement"/> to read the property from.</param>
        /// <param name="propertyName">The name of the property to read.</param>
        /// <returns>The property's string value, or an empty string if missing or null.</returns>
        private static string GetOptionalString(JsonElement data, string propertyName) =>
            data.TryGetProperty(propertyName, out var value) ? value.GetString() ?? "" : "";

        /// <summary>
        /// Attempts to parse the release date from the given game <see cref="JsonElement"/>.
        /// Tolerates a missing "release_date" property, a missing "date" sub-property, and an
        /// unparseable date string, in all cases falling back to <see cref="DateTime.MinValue"/>.
        /// </summary>
        /// <param name="appId">The Steam AppId of the game being parsed, used for logging.</param>
        /// <param name="data">The game's "data" <see cref="JsonElement"/>.</param>
        /// <returns>The parsed <see cref="DateTime"/>, or <see cref="DateTime.MinValue"/> if parsing failed.</returns>
        private DateTime ParseReleaseDate(int appId, JsonElement data)
        {
            if (data.TryGetProperty("release_date", out var releaseDate) &&
                releaseDate.TryGetProperty("date", out var dateStr) &&
                DateTime.TryParse(dateStr.GetString(), out var parsed))
            {
                return parsed;
            }

            var rawDate = data.TryGetProperty("release_date", out var rd) && rd.TryGetProperty("date", out var raw)
                ? raw.GetString()
                : "missing";

            logger.LogWarning("AppId {AppId} has unparseable release date: '{Date}'", appId, rawDate);

            return DateTime.MinValue;
        }
    }
}
