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

var mode = args.Length > 0 ? args[0].ToLowerInvariant() : "csv";
var pathOrNull = args.Length > 1 ? args[1] : null;

builder.Services.AddSingleton<IAppIdSource>(serviceProvider =>
{
    var args = Environment.GetCommandLineArgs();

    var mode = args.Skip(1).FirstOrDefault()?.ToLowerInvariant();

    return mode switch
    {
        "csv" when pathOrNull is not null => new CsvAppIdSource(pathOrNull),
        "full" => serviceProvider.GetRequiredService<SteamAppListSource>(),
        _ => throw new ArgumentException("Invalid args. Usage: csv <path> | steam")
    };
});

using var host = builder.Build();

var appIdSource = host.Services.GetRequiredService<IAppIdSource>();
var runner = host.Services.GetRequiredService<SteamImportRunner>();

var appIds = await appIdSource.GetAppIdsAsync();

Console.WriteLine($"Loaded {appIds.Count()} AppIds");

await runner.ImportGamesAsync(appIds);
