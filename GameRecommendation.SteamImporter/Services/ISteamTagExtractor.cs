using System.Text.Json;

namespace GameRecommendation.SteamImporter.Services
{
    /// <summary>
    /// Extracts steam tags from a Steam API JSON response.
    /// </summary>
    public interface ISteamTagExtractor
    {
        IReadOnlyList<string> Extract(JsonElement data);
    }
}
