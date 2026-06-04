using Microsoft.EntityFrameworkCore;
using VideoGameRecomendationDomain.Models.Domain;

namespace VideoGameRecomendationAPI.Data
{
    public class RecommendationDbContext : DbContext
    {
        public RecommendationDbContext(DbContextOptions<RecommendationDbContext> options) : base(options)
        {
        }

        public DbSet<Game> Games => Set<Game>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<GameTag> GameTags => Set<GameTag>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserRating> UserRatings => Set<UserRating>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureGames(modelBuilder);
            ConfigureTags(modelBuilder);
            ConfigureGameTags(modelBuilder);
            ConfigureUsers(modelBuilder);
            ConfigureUserRatings(modelBuilder);
        }

        private static void ConfigureGames(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasKey(game => game.Id);

                entity.Property(game => game.Name)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(game => game.Description)
                    .IsRequired()
                    .HasMaxLength(5000);

                entity.Property(game => game.ImageUrl)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.HasIndex(game => game.SteamAppId)
                    .IsUnique();

                // ONLY keep navigation declaration (no relationship mapping here)
                entity.HasMany(game => game.GameTags)
                    .WithOne()
                    .HasForeignKey(gameTag => gameTag.GameId);

                entity.HasMany(game => game.UserRatings)
                    .WithOne()
                    .HasForeignKey(userRating => userRating.GameId);
            });
        }

        private static void ConfigureTags(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(tag => tag.Id);

                entity.Property(tag => tag.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(tag => tag.Name)
                    .IsUnique();

                entity.HasMany(tag => tag.GameTags)
                    .WithOne(gameTag => gameTag.Tag)
                    .HasForeignKey(gameTag => gameTag.TagId);
            });
        }

        private static void ConfigureGameTags(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameTag>(entity =>
            {
                entity.HasKey(gameTag => new { gameTag.GameId, gameTag.TagId });

                entity.Property(gameTag => gameTag.Weight)
                    .IsRequired();

                entity.HasOne(gameTag => gameTag.Game)
                    .WithMany(game => game.GameTags)
                    .HasForeignKey(gameTag => gameTag.GameId);

                entity.HasOne(gameTag => gameTag.Tag)
                    .WithMany(tag => tag.GameTags)
                    .HasForeignKey(gameTag => gameTag.TagId);
            });
        }

        private static void ConfigureUsers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(user => user.Id);

                entity.Property(user => user.Username)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasMany(user => user.UserRatings)
                    .WithOne(userRating => userRating.User)
                    .HasForeignKey(userRating => userRating.UserId);
            });
        }

        private static void ConfigureUserRatings(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserRating>(entity =>
            {
                entity.HasKey(userRating => new { userRating.UserId, userRating.GameId });

                entity.Property(userRating => userRating.Rating)
                    .IsRequired();

                entity.Property(userRating => userRating.UpdatedUtc)
                    .IsRequired();

                entity.HasOne(userRating => userRating.User)
                    .WithMany(user => user.UserRatings)
                    .HasForeignKey(userRating => userRating.UserId);

                entity.HasOne(userRating => userRating.Game)
                    .WithMany(game => game.UserRatings)
                    .HasForeignKey(userRating => userRating.GameId);
            });
        }
    }
}
