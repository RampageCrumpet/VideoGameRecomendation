using GameRecomendation.Domain.Models.Domain;
using System.Text.Json;

namespace GameRecomendation.SteamImporter.Services
{
    public interface ISteamGameMapper
    {
        Game? Map(int appId, JsonDocument json);
    }
}