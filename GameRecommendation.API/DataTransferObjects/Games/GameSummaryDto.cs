using GameRecommendation.Domain.Enums;

namespace GameRecommendation.API.DataTransferObjects.Games
{
    /// <summary>
    /// Represents a lightweight summary of a game returned by list endpoints.
    /// Contains identifying and display information (id, name, image, release date)
    /// and the calling user's rating when available.
    /// </summary>
    public class GameSummaryDto
    {
        /// <summary>
        /// The game's unique identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The game's name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The URL to the game's Steam header image.
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// The game's release date.
        /// </summary>
        public DateTime ReleaseDate { get; set; }

        /// <summary>
        /// The calling user's rating for this game, or null if they haven't rated it.
        /// </summary>
        public RatingType? UserRating { get; set; }
    }
}
