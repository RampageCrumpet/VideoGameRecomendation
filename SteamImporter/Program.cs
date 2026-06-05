using GameRecomendation.Infrastructure;
using GameRecomendation.SteamImporter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

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

using var host = builder.Build();

var runner = host.Services.GetRequiredService<SteamImportRunner>();

var appIds = new[]
{
    730,
    570,
    578080,
    1091500,
    1245620
};

await runner.ImportGamesAsync(appIds);