namespace GameRecomendation.Domain.Models.Domain
{
    /// <summary>
    /// This class is a data object that represents a video game.
    /// </summary>
    public class Game
    {
        /// <summary>
        /// The unique identifier for the game. This is the primary key in the database and is used to reference the game in other tables, such as user ratings and game tags.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique identifier for the game on the Steam platform. 
        /// </summary>
        public int SteamAppId { get; set; }

        /// <summary>
        /// The games name.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The games steam description.
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// The games release date.
        /// </summary>
        public DateTime ReleaseDate { get; set; }

        /// <summary>
        /// The URL to the games image on steam.
        /// </summary>
        public required string ImageUrl { get; set; }

        /// <summary>
        /// The collection of tags associated with the game.
        /// </summary>
        public ICollection<GameTag> GameTags { get; set; } = new List<GameTag>();

        /// <summary>
        /// This games user ratings.
        /// </summary>
        public ICollection<UserRating> UserRatings { get; set; } = new List<UserRating>();

    }
}
