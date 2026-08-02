namespace GameRecommendation.Domain.Models.Domain
{
    /// <summary>
    /// Represents a scored game recommendation produced by the recommendation algorithm.
    /// </summary>
    public class RecommendationResult
    {
        /// <summary>
        /// The game being recommended.
        /// </summary>
        public required Game Game { get; set; }

        /// <summary>
        /// The similarity score between the user's preference profile and this game.
        /// </summary>
        public double Score { get; set; }
    }
}
