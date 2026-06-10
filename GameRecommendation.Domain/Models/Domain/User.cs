
namespace GameRecommendation.Domain.Models.Domain
{
    /// <summary>
    /// Represents a user of the game recommendation system.
    /// </summary>
    public class User
    {
        /// <summary>
        /// The user's unique identifier.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The user's display name.
        /// </summary>
        public required string UserName { get; set; }

        /// <summary>
        /// The date and time the user was created, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// The collection of ratings this user has given to games.
        /// </summary>
        public ICollection<UserRating> UserRatings { get; set; } = [];
    }
}
