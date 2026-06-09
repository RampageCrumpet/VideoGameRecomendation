
using Microsoft.AspNetCore.Identity;

namespace GameRecommendation.Domain.Models.Domain
{
    /// <summary>
    /// This class repsents a user of the game recommendation system.
    /// </summary>
    public class User : IdentityUser
    {
        /// <summary>
        /// The date and time the user was created in the system, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// The collection of ratings this user has given to games.
        /// </summary>
        public ICollection<UserRating> UserRatings { get; set; } = [];
    }
}
