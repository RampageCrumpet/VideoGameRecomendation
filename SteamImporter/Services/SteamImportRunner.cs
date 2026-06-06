using GameRecomendation.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameRecomendation.SteamImporter.Services
{
    public class SteamImportRunner
    {
        private readonly ISteamGameFetcher fetcher;
        private readonly ISteamGameMapper mapper;
        private readonly IRecommendationDbContext dataBase;

        public SteamImportRunner(ISteamGameFetcher fetcher, ISteamGameMapper mapper,IRecommendationDbContext dataBase)
        {
            this.fetcher = fetcher;
            this.mapper = mapper;
            this.dataBase = dataBase;
        }

        public async Task ImportGamesAsync(IEnumerable<int> appIds)
        {
            var existingGames = await dataBase.Games
                .ToDictionaryAsync(g => g.SteamAppId);

            foreach (var id in appIds)
            {
                var json = await fetcher.GetGameAsync(id);

                if (json == null)
                {
                    Console.WriteLine($"Failed to fetch app {id}");
                    continue;
                }

                var game = mapper.Map(id, json);

                if (game == null)
                {
                    Console.WriteLine($"Failed to map app {id}");
                    continue;
                }

                if (existingGames.TryGetValue(id, out var existingGame))
                {
                    existingGame.Name = game.Name;
                    existingGame.Description = game.Description;
                    existingGame.ImageUrl = game.ImageUrl;
                    existingGame.ReleaseDate = game.ReleaseDate;
                }
                else
                {
                    dataBase.Games.Add(game);
                    existingGames.Add(id, game);
                }
            }

            await dataBase.SaveChangesAsync();
        }
    }
}
