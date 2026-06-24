using System.Text.Json;

namespace GameRecommendation.SteamImporter.Services
{
    /// <summary>
    /// Extracts tag names from a raw Steam API JSON response.
    /// </summary>
    public class SteamTagExtractor : ISteamTagExtractor
    {
        /// <summary>
        /// Extracts a distinct list of tag names from the genres and categories fields of the given Steam API response.
        /// </summary>
        /// <param name="data">The <see cref="JsonElement"/> containing the game data returned by the Steam store API.</param>
        /// <returns>A distinct, non-empty read-only list of tag name strings.</returns>
        public IReadOnlyList<string> Extract(JsonElement data)
        {
            var tags = new List<string>();

            AddFromArray(data, "genres", tags);
            AddFromArray(data, "categories", tags);

            return tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Extracts tag names from a named array property within the given <see cref="JsonElement"/> and adds them to the provided list.
        /// </summary>
        /// <param name="data">The <see cref="JsonElement"/> to extract from.</param>
        /// <param name="property">The name of the array property to extract tag names from.</param>
        /// <param name="tags">The list to add the extracted tag names to.</param>
        private static void AddFromArray(JsonElement data, string property, List<string> tags)
        {
            if (!data.TryGetProperty(property, out var array))
                return;

            foreach (var item in array.EnumerateArray())
            {
                if (item.TryGetProperty("description", out var desc))
                {
                    var value = desc.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        tags.Add(value);
                }
            }
        }
    }
}
