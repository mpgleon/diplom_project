using diplom_project.Models;
using diplom_project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace diplom_project.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;

        public AuthController(AppDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                return Unauthorized("Invalid email or password");
            }

            var jwtToken = _authService.GenerateJwtToken(user);
            var (refreshToken, refreshTokenExpiry) = await _authService.GenerateRefreshToken(user);

            return Ok(new
            {
                JwtToken = jwtToken,
                RefreshToken = refreshToken,
                Expires = refreshTokenExpiry
            });
        }

        [HttpPost("registration")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = await _authService.RegisterUser(model);
                var jwtToken = _authService.GenerateJwtToken(user);
                var (refreshToken, refreshTokenExpiry) = await _authService.GenerateRefreshToken(user);

                return Ok(new
                {
                    JwtToken = jwtToken,
                    RefreshToken = refreshToken,
                    Expires = refreshTokenExpiry
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest model)
        {
            if (string.IsNullOrEmpty(model.RefreshToken))
                return BadRequest("Refresh token is required.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == model.RefreshToken);

            if (user == null || user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
                return Unauthorized("Invalid or expired refresh token.");

            // Генерируем новый JWT
            var newJwtToken = _authService.GenerateJwtToken(user);

            // Генерируем новый RefreshToken (опционально)
            var (newRefreshToken, expiry) = await _authService.GenerateRefreshToken(user);

            // Возвращаем новый токен и (опционально) новый RefreshToken
            return Ok(new
            {
                jwtToken = newJwtToken,
                refreshToken = newRefreshToken,
                expiry = expiry
            });
        }



    }
    public class LoginModel
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
    public class RegisterModel
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Phone { get; set; }
        [Required]
        public string Password { get; set; }
        public bool IsLandlord { get; set; }

    }
    
}