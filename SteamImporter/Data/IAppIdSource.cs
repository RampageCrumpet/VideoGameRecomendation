
namespace GameRecomendation.SteamImporter.Data
{
    public interface IAppIdSource
    {
        Task<IEnumerable<int>> GetAppIdsAsync();
    }
}
