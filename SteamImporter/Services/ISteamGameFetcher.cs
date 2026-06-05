using System.Text.Json;

namespace GameRecomendation.SteamImporter.Services
{
    public interface ISteamGameFetcher
    {
        Task<JsonDocument?> GetGameAsync(int appId);
    }
}