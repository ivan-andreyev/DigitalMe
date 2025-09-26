using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace DigitalMe.Controllers;

/// <summary>
/// TEMPORARY HARDCODED Authentication controller to bypass PostgreSQL issues
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly ILogger<AccountController> _logger;
    private readonly string _jwtKey;

    public AccountController(ILogger<AccountController> logger, IConfiguration configuration)
    {
        _jwtKey = configuration["JWT_KEY"] ??
                 configuration["JWT:SecretKey"] ??
                 "ThisIsAVerySecretKeyForJWTTokenGenerationAndShouldBeAtLeast32CharactersLong!";
        _logger = logger;
    }

    /// <summary>
    /// TEMPORARY HARDCODED login - bypasses PostgreSQL completely
    /// </summary>
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("🔑 TEMP HARDCODED Login attempt for user: {Email}", model.Email);

            // HARDCODED demo users - NO DATABASE ACCESS
            var validUsers = new Dictionary<string, (string password, string[] roles)>
            {
                { "demo@digitalme.ai", ("Ivan2024!", new[] { "Admin", "User" }) },
                { "mr.red.404@gmail.com", ("Ivan2024!", new[] { "Admin", "User" }) }
            };

            var normalizedEmail = model.Email.ToLower().Trim();
            if (!validUsers.TryGetValue(normalizedEmail, out var userInfo))
            {
                _logger.LogWarning("❌ TEMP User not found: {Email}", model.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (model.Password != userInfo.password)
            {
                _logger.LogWarning("❌ TEMP Password check failed for user: {Email}", model.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            _logger.LogInformation("✅ TEMP Login successful for user: {Email}, Roles: {Roles}",
                model.Email, string.Join(", ", userInfo.roles));

            // Generate JWT token directly
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, model.Email),
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Name, model.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, model.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat,
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                        ClaimValueTypes.Integer64)
                }.Concat(userInfo.roles.Select(role => new Claim(ClaimTypes.Role, role)))),

                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = "DigitalMe.API",
                Audience = "DigitalMe.Client",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                token = tokenString,
                user = new
                {
                    id = Guid.NewGuid().ToString(),
                    email = model.Email,
                    roles = userInfo.roles
                },
                message = "Login successful (TEMPORARY HARDCODED MODE)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ TEMP Error during login for user: {Email}", model.Email);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// TEMPORARY HARDCODED register - just validates input and returns success
    /// </summary>
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("🔑 TEMP HARDCODED Register attempt for user: {Email}", model.Email);

            // Block registration of demo users
            var reservedEmails = new[] { "demo@digitalme.ai", "mr.red.404@gmail.com" };
            if (reservedEmails.Contains(model.Email.ToLower().Trim()))
            {
                return Conflict(new { message = "User with this email already exists" });
            }

            // TEMP: Just return success without actually creating user
            _logger.LogInformation("✅ TEMP Register successful for user: {Email}", model.Email);

            // Generate JWT for new "user"
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, model.Email),
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Name, model.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, model.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, "User")
                }),

                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = "DigitalMe.API",
                Audience = "DigitalMe.Client",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                token = tokenString,
                user = new
                {
                    id = Guid.NewGuid().ToString(),
                    email = model.Email,
                    roles = new[] { "User" }
                },
                message = "Registration successful (TEMPORARY MODE - USER NOT PERSISTED)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ TEMP Error during register for user: {Email}", model.Email);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// TEMPORARY HARDCODED token validation - checks JWT structure
    /// </summary>
    [HttpGet("validate")]
    public IActionResult Validate()
    {
        try
        {
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "Missing or invalid authorization header" });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var tokenHandler = new JwtSecurityTokenHandler();

            // Basic JWT format validation
            if (!tokenHandler.CanReadToken(token))
            {
                return Unauthorized(new { message = "Invalid token format" });
            }

            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Check if token is expired
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Token expired" });
            }

            // Token is valid
            return Ok(new { valid = true, message = "Token is valid" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ TEMP Error during token validation");
            return Unauthorized(new { message = "Invalid token" });
        }
    }
}

public class LoginModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}