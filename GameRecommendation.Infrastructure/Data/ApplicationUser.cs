using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameRecommendation.Infrastructure.Data
{
    /// <summary>
    /// The Identity user entity for authentication. Links to the domain <see cref="GameRecommendation.Domain.Models.Domain.User"/>
    /// via <see cref="DomainUserId"/>.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
    }
}
