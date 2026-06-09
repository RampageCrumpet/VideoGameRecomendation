using System.ComponentModel.DataAnnotations;

namespace GameRecommendation.API.DataTransferObjects.Auth
{
    /// <summary>
    /// The data required to register a new user account.
    /// </summary>
    public class RegisterRequestDto
    {
        /// <summary>
        /// The desired username.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// The desired email address.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The desired password. Must be at least 8 characters and contain an uppercase letter and a digit.
        /// </summary>
        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }
}
