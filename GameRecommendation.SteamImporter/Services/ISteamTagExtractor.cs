using System.Text.Json;

namespace GameRecommendation.SteamImporter.Services
{
    public interface ISteamTagExtractor
    {
        IReadOnlyList<string> Extract(JsonElement data);
    }
}
