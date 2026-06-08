using GameRecommendation.Domain.Enums;

namespace GameRecommendation.Domain.Models.Domain
{
    /// <summary>
    /// Represents a user's opinion of a game.
    /// </summary>
    public class UserRating
    {
        /// <summary>
        /// The Id of the user that made the rating.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Navigation to the user who made the rating.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// The Id of the game that was rated.
        /// </summary>
        public int GameId { get; set; }

        /// <summary>
        /// Navigation to the game being rated.
        /// </summary>
        public Game Game { get; set; } = null!;


        /// <summary>
        /// The rating given by the user.
        /// </summary>
        public RatingType Rating { get; set; }

        /// <summary>
        /// The date and time the rating was created, in UTC.
        /// </summary>
        public DateTime UpdatedUtc { get; set; }
    }
}
