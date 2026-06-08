namespace GameRecommendation.SteamImporter.Data
{
    public class CsvAppIdSource : IAppIdSource
    {
        private readonly string _path;

        public CsvAppIdSource(string path)
        {
            _path = path;
        }

        public async Task<IEnumerable<int>> GetAppIdsAsync()
        {
            var lines = await File.ReadAllLinesAsync(_path);

            var results = new List<int>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var trimmed = line.Trim();

                // skip comments
                if (trimmed.StartsWith("#"))
                    continue;

                // try parse safely (this automatically handles headers too)
                if (int.TryParse(trimmed, out var id))
                {
                    results.Add(id);
                }
            }

            return results;
        }
    }
}
