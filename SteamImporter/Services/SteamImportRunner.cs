using GameRecommendation.Domain.Models.Domain;
using GameRecommendation.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameRecommendation.SteamImporter.Services
{
    /// <summary>
    /// Orchestrates the Steam import pipeline by coordinating data retrieval,
    /// transformation, and persistence of <see cref="Game"/> entities and their associated tags.
    /// </summary>
    public class SteamImportRunner
    {
        private readonly ISteamGameFetcher fetcher;
        private readonly ISteamGameMapper mapper;
        private readonly ISteamTagExtractor tagExtractor;
        private readonly IRecommendationDbContext dataBase;
        private readonly ILogger<SteamImportRunner> logger;

        private readonly Dictionary<int, Game> existingGames;
        private readonly Dictionary<string, Tag> existingTags;

        /// <summary>
        /// Initializes a new instance of the <see cref="SteamImportRunner"/> class.
        /// </summary>
        /// <param name="fetcher">Service responsible for retrieving raw Steam API responses.</param>
        /// <param name="mapper">Transforms raw Steam JSON into <see cref="Game"/> domain entities.</param>
        /// <param name="tagExtractor">Extracts tag metadata (genres, categories) from Steam responses.</param>
        /// <param name="dataBase">Abstraction over the persistence layer for games and tags.</param>
        /// <param name="logger">Logging service for recording import operations and issues.</param>
        public SteamImportRunner(ISteamGameFetcher fetcher, ISteamGameMapper mapper, 
            ISteamTagExtractor tagExtractor, IRecommendationDbContext dataBase, ILogger<SteamImportRunner> logger)
        {
            this.fetcher = fetcher;
            this.mapper = mapper;
            this.tagExtractor = tagExtractor;
            this.dataBase = dataBase;
            this.logger = logger;

            existingGames = new Dictionary<int, Game>();
            existingTags = new Dictionary<string, Tag>();
        }

        /// <summary>
        /// Executes a full import cycle for the provided Steam application IDs.
        /// Existing games are updated, while new games are inserted into the database.
        /// Tags are normalized and reused to prevent duplication.
        /// </summary>
        /// <param name="appIds">Collection of Steam application IDs to import.</param>
        /// <returns>A task representing the asynchronous import operation.</returns>
        public async Task ImportGamesAsync(IEnumerable<int> appIds)
        {
            var idList = appIds.ToList();
            logger.LogInformation("Starting import of {Count} games", idList.Count);

            await LoadCache();
            logger.LogInformation("Cache loaded: {Games} existing games, {Tags} existing tags",
                existingGames.Count, existingTags.Count);

            var succeeded = 0;
            var failed = 0;

            foreach (var id in idList)
            {
                var result = await ProcessGame(id);
                if (result) succeeded++;
                else failed++;
            }

            await dataBase.SaveChangesAsync();

            logger.LogInformation("Import complete. Succeeded: {Succeeded}, Failed: {Failed}",
                succeeded, failed);
        }

        /// <summary>
        /// Loads existing <see cref="Game"/> and <see cref="Tag"/> entities into memory
        /// to reduce repeated database queries during import processing.
        /// </summary>
        /// <returns>A task representing the asynchronous cache loading operation.</returns>
        private async Task LoadCache()
        {
            var games = await dataBase.Games
                .Include(g => g.GameTags)
                    .ThenInclude(gt => gt.Tag)
                .ToListAsync();

            foreach (var game in games)
                existingGames[game.SteamAppId] = game;

            var tags = await dataBase.Tags.ToListAsync();

            foreach (var tag in tags)
                existingTags[tag.Name] = tag;
        }

        /// <summary>
        /// Processes a single Steam application by fetching, mapping, and either
        /// updating or creating the corresponding <see cref="Game"/> entity.
        /// </summary>
        /// <param name="appId">The Steam application ID to process.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task<bool> ProcessGame(int appId)
        {
            var context = await FetchGameContext(appId);
            if (context is null)
                return false;

            if (existingGames.TryGetValue(appId, out var existing))
            {
                UpdateExistingGame(existing, context);
                logger.LogDebug("Updated existing game {AppId} ({Name})", appId, context.Game.Name);
            }
            else
            {
                CreateNewGame(context);
                logger.LogDebug("Created new game {AppId} ({Name})", appId, context.Game.Name);
            }

            return true;
        }

        /// <summary>
        /// Fetches and transforms raw Steam API data into a structured import context
        /// containing both a <see cref="Game"/> entity and its associated tag list.
        /// </summary>
        /// <param name="appId">The Steam application ID to retrieve.</param>
        /// <returns>
        /// A <see cref="GameImportContext"/> containing the mapped game and extracted tags,
        /// or <see langword="null"/> if the fetch or mapping operation fails.
        /// </returns>
        private async Task<GameImportContext?> FetchGameContext(int appId)
        {
            var json = await fetcher.GetGameAsync(appId);
            if (json == null)
            {
                logger.LogWarning("Fetch returned null for appId {AppId}", appId);
                return null;
            }

            var game = mapper.Map(appId, json);
            if (game == null)
            {
                logger.LogWarning("Mapper returned null for appId {AppId}", appId);
                return null;
            }

            var root = json.RootElement.GetProperty(appId.ToString());
            var data = root.GetProperty("data");
            var tags = tagExtractor.Extract(data);

            return new GameImportContext(game, tags);
        }

        /// <summary>
        /// Updates an existing <see cref="Game"/> entity with fresh Steam data
        /// and rebuilds its tag associations.
        /// </summary>
        /// <param name="existing">The existing tracked <see cref="Game"/> entity.</param>
        /// <param name="context">The imported game data and tags.</param>
        private void UpdateExistingGame(Game existing, GameImportContext context)
        {
            existing.Name = context.Game.Name;
            existing.Description = context.Game.Description;
            existing.ImageUrl = context.Game.ImageUrl;
            existing.ReleaseDate = context.Game.ReleaseDate;

            existing.GameTags.Clear();

            ApplyTags(existing, context.Tags);
        }

        /// <summary>
        /// Creates a new <see cref="Game"/> entity and attaches its tag relationships
        /// before adding it to the persistence context.
        /// </summary>
        /// <param name="context">The imported game data and tags.</param>
        private void CreateNewGame(GameImportContext context)
        {
            ApplyTags(context.Game, context.Tags);

            dataBase.Games.Add(context.Game);
            existingGames[context.Game.SteamAppId] = context.Game;
        }

        /// <summary>
        /// Attaches a collection of tags to a <see cref="Game"/>, resolving each tag
        /// through the in-memory cache to prevent duplication.
        /// </summary>
        /// <param name="game">The game to attach tags to.</param>
        /// <param name="tags">The list of tag names to attach.</param>
        private void ApplyTags(Game game, IReadOnlyList<string> tags)
        {
            foreach (var tagName in tags)
            {
                var tag = GetOrCreateTag(tagName);

                game.GameTags.Add(new GameTag
                {
                    Game = game,
                    Tag = tag,
                    Weight = 1.0
                });
            }
        }

        /// <summary>
        /// Retrieves an existing <see cref="Tag"/> from cache or creates a new one
        /// if it does not already exist.
        /// </summary>
        /// <param name="name">The tag name to retrieve or create.</param>
        /// <returns>A tracked <see cref="Tag"/> entity.</returns>
        private Tag GetOrCreateTag(string name)
        {
            if (existingTags.TryGetValue(name, out var tag))
                return tag;

            tag = new Tag { Name = name };
            existingTags[name] = tag;

            return tag;
        }
    }

    /// <summary>
    /// Represents a fully resolved import unit containing both a mapped <see cref="Game"/>
    /// entity and its extracted tag set.
    /// </summary>
    internal record GameImportContext(Game Game, IReadOnlyList<string> Tags);
}
