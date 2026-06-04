namespace VideoGameRecomendationDomain.Models.Domain
{
    public class Reccomendation
    {
        /// <summary>
        /// The userId that the recommendation is for.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// The gameId that is being recommended to the user.
        /// </summary>
        public int GameId { get; set; }

        /// <summary>
        /// A scalar value representing how well the game matches the user's preferences. A higher score indicates a stronger recommendation, while a lower score indicates a weaker recommendation.
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// The date of the reccomendations creation.
        /// </summary>
        public DateTime CreatedUtc { get; set; }
    }
}
