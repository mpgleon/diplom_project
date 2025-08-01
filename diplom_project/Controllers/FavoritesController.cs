using diplom_project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace diplom_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoritesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public FavoritesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("AddFavorite")]
        [Authorize]
        public async Task<IActionResult> AddToFavorites([FromBody] FavoriteModel model)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized("Email claim not found in token.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found.");

            if (model.idListing <= 0)
                return BadRequest("Invalid listing ID.");

            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.idUser == user.Id && f.idListing == model.idListing);
            if (existingFavorite != null)
                return BadRequest("This listing is already in favorites.");

            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == model.idListing);
            if (listing == null)
                return NotFound("Listing not found.");

            var favorite = new Favorite
            {
                idUser = user.Id,
                idListing = model.idListing
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Listing added to favorites", favoriteId = favorite.id });
        }
        

        [HttpGet("GetFavorites")]
        [Authorize]
        public async Task<IActionResult> GetFavoriteListings()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized("Email claim not found in token.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found.");

            var favorites = await _context.Favorites
                .Where(f => f.idUser == user.Id)
                .Include(f => f.Listing)
                .ThenInclude(l => l.ListingPhotos)
                .ThenInclude(lp => lp.Photo)
                .Select(f => new
                {
                    idListing = f.idListing,
                    title = f.Listing.Title,
                    country = f.Listing.Country,
                    city = f.Listing.City,
                    rating = f.Listing.AverageRating,
                    photoUrl = f.Listing.ListingPhotos.FirstOrDefault().Photo.Url
                })
                .ToListAsync();

            if (favorites == null || !favorites.Any())
                return NotFound("No favorite listings found.");


            return Ok(favorites);
        }

        [HttpDelete("DeleteFavorite")]
        [Authorize]
        public async Task<IActionResult> RemoveFromFavorites([FromBody] FavoriteModel model)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized("Email claim not found in token.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found.");

            if (model.idListing <= 0)
                return BadRequest("Invalid listing ID.");

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.idUser == user.Id && f.idListing == model.idListing);
            if (favorite == null)
                return NotFound("Favorite listing not found.");


            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Listing removed from favorites" });
        }

    }

    public class FavoriteModel
    {
        public int idListing { get; set; }
    }

}
