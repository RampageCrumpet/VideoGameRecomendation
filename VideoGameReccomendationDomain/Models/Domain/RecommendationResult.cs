namespace GameRecomendation.Domain.Models.Domain
{
    public class RecommendationResult
    {
        /// <summary>
        /// The game being reccomended.
        /// </summary>
        public required Game Game { get; set; }

        /// <summary>
        /// How strong the game is being reccomended to the user.
        /// </summary>
        public double Score { get; set; }
    }
}
