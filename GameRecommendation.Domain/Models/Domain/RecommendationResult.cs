namespace GameRecommendation.Domain.Models.Domain
{
    public class RecommendationResult
    {
        /// <summary>
        /// The game being recomended.
        /// </summary>
        public required Game Game { get; set; }

        /// <summary>
        /// How strong the game is being recomended to the user.
        /// </summary>
        public double Score { get; set; }
    }
}
