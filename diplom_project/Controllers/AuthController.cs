using diplom_project.Models;
using diplom_project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .ThenInclude(up => up.UserProfileLanguages)
                .ThenInclude(upl => upl.Language)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null) return NotFound("User not found");

            if (user.UserProfile == null) return NotFound("Profile not found");

            var profile = new
            {
                user.UserProfile.FirstName,
                user.UserProfile.LastName,
                user.UserProfile.Email,
                user.UserProfile.Phone,
                user.UserProfile.DateOfBirth,
                Languages = user.UserProfile.UserProfileLanguages.Select(upl => new { upl.Language.Code, upl.Language.Name }),
                user.UserProfile.IsVerified,
                user.UserProfile.Rating,
                user.UserProfile.Description,
                user.UserProfile.PhotoUrl
            };

            return Ok(profile);
        }

        [HttpPost("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileModel model)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .ThenInclude(up => up.UserProfileLanguages)
                .ThenInclude(upl => upl.Language)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || user.UserProfile == null)
                return NotFound("Profile not found");

            // Обновление профиля
            user.UserProfile.FirstName = model.FirstName;
            user.UserProfile.LastName = model.LastName;
            user.UserProfile.DateOfBirth = model.DateOfBirth;
            user.UserProfile.Description = model.Description;
            user.UserProfile.PhotoUrl = model.PhotoUrl;

            // Удаление существующих языков с защитой от null
            if (user.UserProfile.UserProfileLanguages != null && user.UserProfile.UserProfileLanguages.Any())
            {
                _context.UserProfileLanguages.RemoveRange(user.UserProfile.UserProfileLanguages);
            }

            // Добавление новых языков
            if (model.LanguageCodes != null)
            {
                foreach (var languageCode in model.LanguageCodes)
                {
                    var language = await _context.Languages.FirstOrDefaultAsync(l => l.Code == languageCode);
                    if (language != null)
                    {
                        user.UserProfile.UserProfileLanguages.Add(new UserProfileLanguage
                        {
                            LanguageId = language.Id,
                            UserProfileId = user.UserProfile.Id
                        });
                    }
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully" });
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
        public string FirstName { get; set; } // Добавлено
        public string LastName { get; set; }  // Добавлено
    }
    public class ProfileModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public List<string>? LanguageCodes { get; set; }
        public string? Description { get; set; }
        public string? PhotoUrl { get; set; }
    }
}