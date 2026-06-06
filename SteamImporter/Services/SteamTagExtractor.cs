using System.Text.Json;

namespace GameRecomendation.SteamImporter.Services
{
    public class SteamTagExtractor : ISteamTagExtractor
    {
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
