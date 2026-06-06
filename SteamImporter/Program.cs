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

builder.Services.AddHttpClient<SteamGameFetcher>();

builder.Services.AddTransient<SteamGameMapper>();

builder.Services.AddTransient<SteamImportRunner>();

var csvPath = args.Length > 0
    ? args[0]
    : "appids.csv";

builder.Services.AddSingleton<IAppIdSource>(
    new CsvAppIdSource(csvPath));

using var host = builder.Build();

var runner = host.Services.GetRequiredService<SteamImportRunner>();

var appIdSource = host.Services.GetRequiredService<IAppIdSource>();

var appIds = await appIdSource.GetAppIdsAsync();

Console.WriteLine($"Loaded {appIds.Count()} AppIds");

await runner.ImportGamesAsync(appIds);
