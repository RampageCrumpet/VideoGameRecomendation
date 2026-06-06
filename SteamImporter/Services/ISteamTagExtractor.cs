using System.Text.Json;

namespace GameRecomendation.SteamImporter.Services
{
    public interface ISteamTagExtractor
    {
        IReadOnlyList<string> Extract(JsonElement data);
    }
}