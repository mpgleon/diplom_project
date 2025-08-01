using diplom_project.DTO;
using diplom_project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace diplom_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }

        //for Landlord
        [HttpGet("received-user-reviews")]
        [Authorize]
        public async Task<IActionResult> GetReceivedUserReviews()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Email claim not found in token.");
            }

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var reviews = await _context.RatingListUsers
                .Where(ru => ru.UserId2 == user.Id) // Отзывы о текущем пользователе
                .Include(ru => ru.Reviewer)
                .ThenInclude(r => r.UserProfile)
                .Select(ru => new
                {
                    reviewerPhotoUrl = ru.Reviewer.UserProfile.PhotoUrl,
                    reviewerName = ru.Reviewer.UserProfile.FirstName + " " + ru.Reviewer.UserProfile.LastName,
                    rating = ru.Rating,
                    datestamp = ru.CreatedDate,
                    description = ru.Description
                })
                .ToListAsync();

            if (reviews == null || !reviews.Any())
            {
                return NotFound("No reviews found for this user.");
            }
            return Ok(reviews);
        }

        //for Landlord
        [HttpGet("received-listing-reviews")]
        [Authorize]
        public async Task<IActionResult> GetGivenListingReviews()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("Email claim not found in token.");
            }

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var reviews = await _context.Listings
            .Where(l => l.UserId == user.Id) // Наши объявления
            .Include(l => l.RatingListListings)
                .ThenInclude(rl => rl.Reviewer)
                .ThenInclude(r => r.UserProfile)
            .Include(l => l.ListingPhotos)
                .ThenInclude(lp => lp.Photo)
            .SelectMany(l => l.RatingListListings) // Все отзывы к нашим объявлениям
            .Select(rl => new
            {
                listingTitle = rl.Listing.Title,
                listingPhotoUrl = rl.Listing.ListingPhotos.FirstOrDefault().Photo.Url, // Первая фотография листинга
                reviewerName = rl.Reviewer.UserProfile.FirstName + " " + rl.Reviewer.UserProfile.LastName,
                reviewerPhotoUrl = rl.Reviewer.UserProfile.PhotoUrl,
                reviewerRating = rl.Reviewer.UserProfile.Rating, // Рейтинг отзыводателя
                rating = rl.Rating, // Рейтинг, поставленный нашему объявлению
                datestamp = rl.CreatedDate,
                description = rl.Description
            })
            .ToListAsync();

            if (reviews == null || !reviews.Any())
            {
                return NotFound("No reviews found for this user's listings.");
            }

            return Ok(reviews);
        }
    }
}

