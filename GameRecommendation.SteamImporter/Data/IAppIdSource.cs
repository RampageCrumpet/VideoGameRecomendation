
namespace GameRecommendation.SteamImporter.Data
{
    /// <summary>
    /// Provides a collection of Steam application IDs to import.
    /// </summary>
    public interface IAppIdSource
    {
        Task<IEnumerable<int>> GetAppIdsAsync();
    }
}
