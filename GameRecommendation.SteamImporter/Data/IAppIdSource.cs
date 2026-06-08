
namespace GameRecommendation.SteamImporter.Data
{
    public interface IAppIdSource
    {
        Task<IEnumerable<int>> GetAppIdsAsync();
    }
}
