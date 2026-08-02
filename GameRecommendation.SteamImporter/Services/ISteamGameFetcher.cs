using System.Text.Json;

namespace GameRecommendation.SteamImporter.Services
{
    /// <summary>
    /// Fetches raw game data from the Steam store API for a given application ID.
    /// </summary>
    public interface ISteamGameFetcher
    {
        Task<JsonDocument?> GetGameAsync(int appId);
    }
}
