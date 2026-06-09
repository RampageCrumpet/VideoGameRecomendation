namespace GameRecommendation.API.DataTransferObjects.Recommendations
{
    /// <summary>
    /// A single game recommendation with its similarity score.
    /// </summary>
    public class RecommendationDto
    {
        /// <summary>
        /// The recommended game's unique identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The recommended game's name.
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
        /// The cosine similarity score between the user's preference profile and this game.
        /// A value closer to 1.0 indicates a stronger match.
        /// </summary>
        public double Score { get; set; }
    }
}
