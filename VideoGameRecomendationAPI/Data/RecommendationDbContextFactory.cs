using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VideoGameRecomendationAPI.Data
{
    public class RecommendationDbContextFactory : IDesignTimeDbContextFactory<RecommendationDbContext>
    {
        public RecommendationDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json")
                            .Build();

            var optionsBuilder = new DbContextOptionsBuilder<RecommendationDbContext>();

            optionsBuilder.UseSqlServer(
                config.GetConnectionString("DefaultConnection")
            );

            return new RecommendationDbContext(optionsBuilder.Options);
        }
    }
}
