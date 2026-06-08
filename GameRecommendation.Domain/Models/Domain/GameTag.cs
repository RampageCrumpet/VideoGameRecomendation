namespace GameRecommendation.Domain.Models.Domain
{
    /// <summary>
    /// This class represents the relationship between a game and a tag.
    /// </summary>
    public class GameTag
    {
        /// <summary>
        /// The game's Id in the database.
        /// </summary>
        public int GameId { get; set; }

        /// <summary>
        /// Navigation to the game that is associated with the tag.
        /// </summary>
        public Game Game { get; set; } = null!;

        /// <summary>
        /// The tag's Id in the database.
        /// </summary>
        public int TagId { get; set; }

        /// <summary>
        /// Navigation to the tag that is associated with the game.
        /// </summary>
        public Tag Tag { get; set; } = null!;

        /// <summary>
        /// The weight of the tag for the game. This is a scalar value that represents how strongly the tag is associated with the game. A higher weight indicates a stronger association, while a lower weight indicates a weaker association.
        /// </summary>
        public double Weight { get; set; }
    }
}
