using diplom_project.Models;
using diplom_project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace diplom_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
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
                roles = user.UserRoles?.Select(ur => ur.Role.Name),
                user.Balance
            };

            return Ok(profileResponse);
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

        [HttpPost]
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
            user.UserProfile.Location = model.Location;
            user.UserProfile.DateOfBirth = model.DateOfBirth;
            user.UserProfile.Description = model.Description;
            user.UserProfile.PhotoUrl = model.PhotoUrl;
            user.UserProfile.Instagram = model.Instagram;
            user.UserProfile.Facebook = model.Facebook;
            user.UserProfile.Telegram = model.Telegram;

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
        [Required]
        public List<string>? LanguageCodes { get; set; }
        public string? Description { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Instagram { get; set; }
        public string? Facebook { get; set; }
        public string? Telegram { get; set; }
    }
    public class UserRatingModel
    {
        public int UserId2 { get; set; } // Кому оставлен отзыв
        public string Description { get; set; }
        public decimal Rating { get; set; }
    }
}
