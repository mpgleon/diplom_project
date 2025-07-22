using diplom_project.Models;
using diplom_project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;

namespace diplom_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfileController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet("get")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .ThenInclude(up => up.UserProfileLanguages)
                .ThenInclude(upl => upl.Language)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || user.UserProfile == null)
                return NotFound("Profile not found");

            var profileResponse = new
            {
                user.UserProfile.FirstName,
                user.UserProfile.LastName,
                user.UserProfile.Surname,
                user.UserProfile.Location,
                user.Email,
                user.Phone,
                user.UserProfile.DateOfBirth,
                languages = user.UserProfile.UserProfileLanguages?.Select(upl => new { upl.Language.Code, upl.Language.Name }),
                user.UserProfile.IsVerified,
                user.UserProfile.Rating,
                user.UserProfile.Description,
                user.UserProfile.PhotoUrl,
                user.UserProfile.Instagram,
                user.UserProfile.Facebook,
                user.UserProfile.Telegram,
                user.UserProfile.Gender,
                roles = user.UserRoles?.Select(ur => ur.Role.Name)
                
            };
            if (profileResponse == null)
                return NotFound(new { error = "Profile not found" });

            return Ok(profileResponse);
        }

        [HttpGet("get-avatar/{filePath}")]
        [AllowAnonymous] // Доступен без авторизации, так как путь публичный
        public IActionResult GetAvatar(string filePath)
        {
            // Санитизация пути
            var sanitizedPath = filePath.TrimStart('/').Replace("../", ""); // Предотвращаем выход за пределы
            var fullPath = Path.Combine(_env.WebRootPath, "uploads", sanitizedPath); // Добавляем "uploads" вручную

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound($"Image not found at: {fullPath}");
            }

            var mimeType = "image/jpeg";
            if (sanitizedPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                mimeType = "image/png";
            else if (sanitizedPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || sanitizedPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                mimeType = "image/jpeg";

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return new FileStreamResult(fileStream, mimeType);
        }
        [HttpGet("get-all-avatars")]
        [Authorize]
        public async Task<IActionResult> GetAllAvatars()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            var avatars = await _context.Users
                .Where(u => u.UserProfile != null && u.UserProfile.PhotoUrl != null)
                .Select(u => new { u.UserProfile.PhotoUrl })
                .ToListAsync();

            return Ok(avatars);
        }
        [HttpGet("get-balance")]
        [Authorize]
        public async Task<IActionResult> GetBalance()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            return Ok(new { Balance = user.Balance });
        }
        [HttpPost("upload-avatar")]
        [Authorize]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || user.UserProfile == null)
                return NotFound("User not found");

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (!file.ContentType.StartsWith("image/"))
                return BadRequest("Only image files are allowed.");

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Сохраняем только относительный путь от wwwroot
            user.UserProfile.PhotoUrl = $"avatars\\{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { photoUrl = user.UserProfile.PhotoUrl });
        }

        [HttpPost("ratings/user")]
        [Authorize]
        public async Task<IActionResult> CreateUserRating([FromBody] UserRatingModel model)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            var ratedUser = await _context.Users.FindAsync(model.UserId2);
            if (ratedUser == null)
                return BadRequest("Rated user not found");

            var existingRating = await _context.RatingListUsers
                .FirstOrDefaultAsync(rlu => rlu.UserId1 == user.Id && rlu.UserId2 == model.UserId2);
            if (existingRating != null)
                return BadRequest("You have already left a review for this user.");

            var rating = new RatingListUser
            {
                UserId1 = user.Id,
                UserId2 = model.UserId2,
                Description = model.Description,
                Rating = model.Rating,
                CreatedDate = DateTime.UtcNow
            };

            _context.RatingListUsers.Add(rating);
            await _context.SaveChangesAsync();

            // Обновляем рейтинг пользователя
            var ratingService = _context.GetService<IRatingService>();
            await ratingService.UpdateUserRatingAsync(model.UserId2);

            return Ok(new { message = "Rating added successfully" });
        }

        [HttpPost("update")]
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
            user.UserProfile.Surname = model.Surname;
            user.UserProfile.Email = model.Email;
            user.Email = model.Email;
            user.UserProfile.Phone = model.Phone;
            user.Phone = model.Phone;
            user.UserProfile.Location = model.Location;
            user.UserProfile.DateOfBirth = model.DateOfBirth;
            user.UserProfile.Description = model.Description;
            user.UserProfile.Instagram = model.Instagram;
            user.UserProfile.Facebook = model.Facebook;
            user.UserProfile.Telegram = model.Telegram;
            user.UserProfile.Gender = model.Gender;

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
    public class ProfileModel
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Surname { get; set; }
        [Required]
        public string Location { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; }
        [Required]
        public List<string>? LanguageCodes { get; set; }
        public string? Description { get; set; }
        public string? Instagram { get; set; }
        public string? Facebook { get; set; }
        public string? Telegram { get; set; }
        public string? Gender { get; set; }
        public string Phone { get; set; }
    }
    public class UserRatingModel
    {
        public int UserId2 { get; set; } // Кому оставлен отзыв
        public string Description { get; set; }
        public decimal Rating { get; set; }
    }
}
