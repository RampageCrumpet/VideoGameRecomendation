namespace GameRecommendation.Domain.Models.Domain
{
    /// <summary>
    /// This class repsents a user of the game recommendation system.
    /// </summary>
    public class User
    {
        /// <summary>
        /// The users Id in the database.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The display name for the user.
        /// </summary>
        public required string Username { get; set; }

        /// <summary>
        /// The date and time the user was created in the system, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// The collection of ratings this user has given to games.
        /// </summary>
        public ICollection<UserRating> UserRatings { get; set; } = [];
    }
}
