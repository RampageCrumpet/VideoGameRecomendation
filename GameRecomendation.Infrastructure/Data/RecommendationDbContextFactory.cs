using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GameRecomendation.Infrastructure
{
    public class RecommendationDbContextFactory : IDesignTimeDbContextFactory<RecommendationDbContext>
    {
        public RecommendationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RecommendationDbContext>();

            optionsBuilder.UseSqlServer("Server=localhost;Database=GameRecomendation;Trusted_Connection=True;TrustServerCertificate=True");

            return new RecommendationDbContext(optionsBuilder.Options);
        }
    }
}
