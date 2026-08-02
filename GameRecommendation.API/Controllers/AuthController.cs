using GameRecommendation.API.DataTransferObjects.Auth;
using GameRecommendation.Domain.Models.Domain;
using GameRecommendation.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
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
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;
        private readonly IRecommendationDbContext dbContext;
        private readonly ILogger<AuthController> logger;

        public AuthController(UserManager<ApplicationUser> userManager, IRecommendationDbContext dbContext, IConfiguration configuration, ILogger<AuthController> logger)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
            this.configuration = configuration;
            this.logger = logger;
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        /// <param name="request">The registration details.</param>
        /// <returns>A JWT token on success, or validation errors on failure.</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            var applicationUser = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email
            };

            var result = await userManager.CreateAsync(applicationUser, request.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            var domainUser = new User
            {
                Id = applicationUser.Id,
                UserName = request.UserName,
                CreatedUtc = DateTime.UtcNow
            };

            dbContext.RatingUsers.Add(domainUser);
            await dbContext.SaveChangesAsync();

            var (token, expiresUtc) = GenerateJwtToken(applicationUser);

            return CreatedAtAction(nameof(Register), new AuthResponseDto
            {
                Token = token,
                ExpiresUtc = expiresUtc,
                UserName = applicationUser.UserName!
            });
        }

        /// <summary>
        /// Logs in to an existing user account.
        /// </summary>
        /// <param name="request">The login credentials.</param>
        /// <returns>A JWT token on success, or 401 on failure.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            try
            {
                var user = await userManager.FindByEmailAsync(request.Email);

                if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
                    return Unauthorized(new { error = "Invalid email or password." });

                var (token, expiresUtc) = GenerateJwtToken(user);

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    ExpiresUtc = expiresUtc,
                    UserName = user.UserName!
                });
            }
            catch (InvalidOperationException ex)
            {
                // multiple users found for the given email
                logger.LogError(ex, "Multiple users with same email found: {Email}", request.Email);
                return Problem(detail: "Multiple accounts exist for this email address. Please contact support.", statusCode: StatusCodes.Status400BadRequest);
            }
        }

        private (string token, DateTime expiresUtc) GenerateJwtToken(ApplicationUser user)
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
