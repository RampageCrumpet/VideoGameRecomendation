namespace GameRecommendation.SteamImporter.Data
{
    /// <summary>
    /// Provides Steam application IDs loaded from a CSV file.
    /// Supports comments prefixed with '#' and automatically skips blank lines and non-numeric entries.
    /// </summary>
    public class CsvAppIdSource : IAppIdSource
    {
        private readonly string path;

        /// <summary>
        /// Initializes a new instance of <see cref="CsvAppIdSource"/> with the path to the CSV file.
        /// </summary>
        /// <param name="path">The path to the CSV file containing Steam application IDs.</param>
        public CsvAppIdSource(string path)
        {
            this.path = path;
        }

        /// <summary>
        /// Reads the CSV file and returns the collection of valid Steam application IDs.
        /// </summary>
        /// <returns>A collection of Steam application IDs parsed from the file.</returns>
        public async Task<IEnumerable<int>> GetAppIdsAsync()
        {
            var lines = await File.ReadAllLinesAsync(path);

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
