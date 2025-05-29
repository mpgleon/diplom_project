using diplom_project.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    }

    public class LoginModel
    {
        public string Email { get; set; }
        public string? Phone { get; set; }
        public string Password { get; set; }
    }
    public class RegisterModel
    {
        public string Email { get; set; }
        public string? Phone { get; set; }
        public string Password { get; set; }
    }
}