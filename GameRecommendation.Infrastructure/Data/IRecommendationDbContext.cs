using GameRecommendation.Domain.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace GameRecommendation.Infrastructure.Data
{
    public interface IRecommendationDbContext
    {
        DbSet<Game> Games { get; }
        DbSet<GameTag> GameTags { get; }
        DbSet<Tag> Tags { get; }
        DbSet<UserRating> UserRatings { get; }
        DbSet<User> Users { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
