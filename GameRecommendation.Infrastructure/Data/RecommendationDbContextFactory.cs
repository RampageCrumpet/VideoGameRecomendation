using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GameRecommendation.Infrastructure
{
    public class RecommendationDbContextFactory : IDesignTimeDbContextFactory<RecommendationDbContext>
    {
        public RecommendationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RecommendationDbContext>();

            optionsBuilder.UseSqlServer("Server=localhost;Database=GameRecommendation;Trusted_Connection=True;TrustServerCertificate=True");

            return new RecommendationDbContext(optionsBuilder.Options);
        }
    }
}
