using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Diagnostics.CodeAnalysis;

namespace GameRecommendation.Infrastructure.Data
{
    /// <summary>
    /// Creates a <see cref="RecommendationDbContext"/> configured for design-time use.
    /// </summary>
    /// <param name="args">Command-line arguments passed by EF Core tooling. Not used.</param>
    /// <returns>A <see cref="RecommendationDbContext"/> configured with the local SQL Server connection string.</returns>
    [ExcludeFromCodeCoverage(Justification = "Design-time only factory used by EF Core tooling for migrations. Not executed at runtime or during testing.")]
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
