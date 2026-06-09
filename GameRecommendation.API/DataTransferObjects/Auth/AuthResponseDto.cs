namespace GameRecommendation.API.DataTransferObjects.Auth
{
    /// <summary>
    /// The response returned on successful authentication.
    /// </summary>
    public class AuthResponseDto
    {
        /// <summary>
        /// The JWT bearer token to use for authenticated requests.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// The UTC date and time at which the token expires.
        /// </summary>
        public DateTime ExpiresUtc { get; set; }

        /// <summary>
        /// The authenticated user's username.
        /// </summary>
        public string UserName { get; set; } = string.Empty;
    }
}
