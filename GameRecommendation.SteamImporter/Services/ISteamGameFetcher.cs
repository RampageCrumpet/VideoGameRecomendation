using System.Text.Json;

namespace GameRecommendation.SteamImporter.Services
{
    public interface ISteamGameFetcher
    {
        Task<JsonDocument?> GetGameAsync(int appId);
    }
}
