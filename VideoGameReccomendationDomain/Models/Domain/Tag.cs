namespace VideoGameRecomendationDomain.Models.Domain
{
    /// <summary>
    /// Represents a steam tag.
    /// </summary>
    public class Tag
    {
        /// <summary>
        /// The tags Id in the database.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The tags name, e.g. "Action", "Indie", "RPG", etc.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The collection of game-tag relationships that reference this tag.
        /// </summary>
        public ICollection<GameTag> GameTags { get; set; } = [];
    }
}
