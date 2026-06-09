using GameRecommendation.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace GameRecommendation.API.DataTransferObjects.Ratings
{
    /// <summary>
    /// The data required to create or update a game rating.
    /// </summary>
    public class UpsertRatingRequestDto
    {
        /// <summary>
        /// The ID of the game to rate.
        /// </summary>
        [Required]
        public int GameId { get; set; }

        /// <summary>
        /// The rating to assign to the game.
        /// </summary>
        [Required]
        [EnumDataType(typeof(RatingType))]
        public RatingType Rating { get; set; }
    }
}
