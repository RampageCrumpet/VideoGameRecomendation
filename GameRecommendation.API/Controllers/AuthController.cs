using GameRecommendation.API.DataTransferObjects.Auth;
using GameRecommendation.Domain.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GameRecommendation.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> userManager;
        private readonly IConfiguration configuration;

        public AuthController(UserManager<User> userManager, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.configuration = configuration;
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        /// <param name="request">The registration details.</param>
        /// <returns>A JWT token on success, or validation errors on failure.</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            var user = new User
            {
                UserName = request.UserName,
                Email = request.Email,
                CreatedUtc = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            var (token, expiresUtc) = GenerateJwtToken(user);

            return CreatedAtAction(nameof(Register), new AuthResponseDto
            {
                Token = token,
                ExpiresUtc = expiresUtc,
                UserName = user.UserName!
            });
        }

        /// <summary>
        /// Logs in to an existing user account.
        /// </summary>
        /// <param name="request">The login credentials.</param>
        /// <returns>A JWT token on success, or 401 on failure.</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
                return Unauthorized();

            var (token, expiresUtc) = GenerateJwtToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                ExpiresUtc = expiresUtc,
                UserName = user.UserName!
            });
        }

        private (string token, DateTime expiresUtc) GenerateJwtToken(User user)
        {
            var jwtKey = configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT key is not configured.");

            var claims = new[]
            {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Name, user.UserName!)
                };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresUtc = DateTime.UtcNow.AddHours(8);

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresUtc,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresUtc);
        }
    }
}
