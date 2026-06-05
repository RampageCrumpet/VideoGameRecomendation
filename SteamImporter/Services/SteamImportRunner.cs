using GameRecomendation.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameRecomendation.SteamImporter.Services
{
    public class SteamImportRunner
    {
        private readonly SteamGameFetcher fetcher;
        private readonly SteamGameMapper mapper;
        private readonly IRecommendationDbContext dataBase;

        public SteamImportRunner(SteamGameFetcher fetcher, SteamGameMapper mapper,IRecommendationDbContext dataBase)
        {
            this.fetcher = fetcher;
            this.mapper = mapper;
            this.dataBase = dataBase;
        }

        public async Task ImportGamesAsync(IEnumerable<int> appIds)
        {
            var existingIds = await dataBase.Games
                           .Select(g => g.SteamAppId)
                           .ToListAsync();

            foreach (var id in appIds)
            {
                if (existingIds.Contains(id))
                    continue;

                var json = await fetcher.GetGameAsync(id);
                if (json == null) continue;

                var game = mapper.Map(id, json);
                if (game == null) continue;

                dataBase.Games.Add(game);

                existingIds.Add(id);
            }

            await dataBase.SaveChangesAsync();
        }
    }
}
