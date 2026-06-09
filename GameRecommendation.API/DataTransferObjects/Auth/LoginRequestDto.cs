using System.ComponentModel.DataAnnotations;

namespace GameRecommendation.API.DataTransferObjects.Auth
{
    /// <summary>
    /// The data required to log in to an existing user account.
    /// </summary>
    public class LoginRequestDto
    {
        /// <summary>
        /// The user's email address.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The user's password.
        /// </summary>
        [Required]
        public string Password { get; set; } = string.Empty;

    }
}
