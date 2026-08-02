using GameRecommendation.Domain.Models.Domain;
using System.Text.Json;

namespace GameRecommendation.SteamImporter.Services
{
    /// <summary>
    /// Maps raw Steam API JSON responses to <see cref="Game"/> domain models.
    /// </summary>
    public interface ISteamGameMapper
    {
        Game? Map(int appId, JsonDocument json);
    }
}
