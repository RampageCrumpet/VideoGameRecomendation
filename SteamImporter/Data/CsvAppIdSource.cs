namespace GameRecomendation.SteamImporter.Data
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

            return lines
                .Skip(1)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(int.Parse);
        }
    }
}
