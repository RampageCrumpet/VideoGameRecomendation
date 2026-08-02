using Microsoft.AspNetCore.Identity;

namespace GameRecommendation.Infrastructure.Data
{
    /// <summary>
    /// The Identity user entity for authentication. Links to the domain
    /// <see cref="GameRecommendation.Domain.Models.Domain.User"/> via a shared ID.
    /// Added to make the addition of future application-specific properties easier, if needed./>.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
    }
}
