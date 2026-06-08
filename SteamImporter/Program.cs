using GameRecommendation.Infrastructure;
using GameRecommendation.SteamImporter.Data;
using GameRecommendation.SteamImporter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<RecommendationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<IRecommendationDbContext>(
    provider => provider.GetRequiredService<RecommendationDbContext>());

builder.Services.AddHttpClient<ISteamGameFetcher, SteamGameFetcher>();
builder.Services.AddHttpClient<SteamAppListSource>();

builder.Services.AddTransient<ISteamGameMapper, SteamGameMapper>();
builder.Services.AddTransient<ISteamTagExtractor, SteamTagExtractor>();
builder.Services.AddTransient<SteamImportRunner>();

var connString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Connection string: {connString}");
Console.Out.Flush();

using var host = builder.Build();

var mode = args.Length > 0
    ? args[0].ToLowerInvariant()
    : "csv";

var path = args.Length > 1
    ? args[1]
    : null;

IAppIdSource appIdSource;
try
{
    appIdSource = mode switch
    {
        "csv" when path is not null
            => File.Exists(path)
                ? ActivatorUtilities.CreateInstance<CsvAppIdSource>(host.Services, path)
                : throw new ArgumentException($"CSV file not found: '{path}'"),

        "full"
            => host.Services.GetRequiredService<SteamAppListSource>(),

        _ => throw new ArgumentException("Usage: csv <path> | full")
    };
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"[SteamImporter] {ex.Message}");
    return;
}

var runner = host.Services.GetRequiredService<SteamImportRunner>();

var appIds = await appIdSource.GetAppIdsAsync();

Console.WriteLine($"Loaded {appIds.Count()} AppIds");

await runner.ImportGamesAsync(appIds);

Console.WriteLine("Finished importing games.");