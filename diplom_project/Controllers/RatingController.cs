using diplom_project.Models;
using diplom_project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Security.Claims;


namespace diplom_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RatingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RatingController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("listing")]
        [Authorize]
        public async Task<IActionResult> CreateListingRating([FromBody] RatingModel model)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            var listing = await _context.Listings.FindAsync(model.ListingId);
            if (listing == null)
                return BadRequest("Listing not found");
            // Проверка на существующий отзыв
            var existingRating = await _context.RatingListListings
                .FirstOrDefaultAsync(rll => rll.UserId == user.Id && rll.ListingId == model.ListingId);
            if (existingRating != null)
                return BadRequest("You have already left a review for this listing.");

            var rating = new RatingListListing
            {
                UserId = user.Id,
                ListingId = model.ListingId,
                Description = model.Description,
                Rating = model.Rating,
                CreatedDate = DateTime.UtcNow
            };

            _context.RatingListListings.Add(rating);
            await _context.SaveChangesAsync();

            // Обновляем рейтинг объявления
            var ratingService = _context.GetService<IRatingService>();
            await ratingService.UpdateListingRatingAsync(model.ListingId);

            return Ok(new { message = "Rating added successfully" });
        }

        [HttpPost("user")]
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
        public class RatingModel
        {
            public int ListingId { get; set; }
            public string Description { get; set; }
            public decimal Rating { get; set; }
        }
        public class UserRatingModel
        {
            public int UserId2 { get; set; } // Кому оставлен отзыв
            public string Description { get; set; }
            public decimal Rating { get; set; }
        }
    }
}
