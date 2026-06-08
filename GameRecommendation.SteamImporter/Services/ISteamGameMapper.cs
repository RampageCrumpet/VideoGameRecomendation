using GameRecommendation.Domain.Models.Domain;
using System.Text.Json;

namespace GameRecommendation.SteamImporter.Services
{
    public interface ISteamGameMapper
    {
        Game? Map(int appId, JsonDocument json);
    }
}
