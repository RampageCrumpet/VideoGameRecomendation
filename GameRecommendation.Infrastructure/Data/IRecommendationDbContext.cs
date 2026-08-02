using GameRecommendation.Domain.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace GameRecommendation.Infrastructure.Data
{
    /// <summary>
    /// Defines the database context interface for the recommendation system.
    /// </summary>
    public interface IRecommendationDbContext
    {
        /// <summary>The games available for recommendation.</summary>
        DbSet<Game> Games { get; }

        /// <summary>The join table between games and tags.</summary>
        DbSet<GameTag> GameTags { get; }

        /// <summary>The tags used to categorize games.</summary>
        DbSet<Tag> Tags { get; }

        /// <summary>The domain users who have rated games.</summary>
        DbSet<User> RatingUsers { get; }

        /// <summary>The ratings users have given to games.</summary>
        DbSet<UserRating> UserRatings { get; }

        /// <summary>Saves all pending changes to the database.</summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
