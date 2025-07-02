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
    public class ListingController : ControllerBase
    {

        private readonly AppDbContext _context;

        public ListingController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("amenities")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAmenities()
        {
            var amenities = await _context.Amenities.ToListAsync();
            return Ok(amenities);
        }

        [HttpGet("main-features")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetMainFeatures()
        {
            var mainFeatures = await _context.MainFeatures.ToListAsync();
            return Ok(mainFeatures);
        }

        [HttpPost("ratings/listing")]
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

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetListing(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.HouseType)
                .Include(l => l.ListingPhotos)
                    .ThenInclude(lp => lp.Photo)
                .Include(l => l.ListingAmenities)
                    .ThenInclude(la => la.Amenity)
                .Include(l => l.ListingMainFeatures)
                    .ThenInclude(lmf => lmf.MainFeature)
                .Include(l => l.User)
                    .ThenInclude(u => u.UserProfile)
                .Include(l => l.RatingListListings)
                    .ThenInclude(rll => rll.Reviewer)
                        .ThenInclude(r => r.UserProfile)
                .Where(l => l.Id == id)
                .Select(l => new
                {
                    
                    HouseType = l.HouseType.Name,
                    Photos = l.ListingPhotos.Select(lp => lp.Photo.Url).ToList(),
                    l.Title,
                    l.Location,
                    Price = l.PerWeek ?? l.PerDay ?? l.PerMonth,
                    Amenities = l.ListingAmenities.Select(la => la.Amenity.Name).ToList(),
                    MainFeatures = l.ListingMainFeatures.Select(lmf => new { lmf.MainFeature.Name, lmf.Value }).ToList(),
                    l.AverageRating,
                    CheckInTime = l.CheckInTime.ToString(@"hh\:mm"), 
                    CheckOutTime = l.CheckOutTime.ToString(@"hh\:mm"),
                    Landlord = new
                    {
                        l.User.UserProfile.FirstName,
                        l.User.UserProfile.LastName,
                        l.User.UserProfile.PhotoUrl,
                        l.User.UserProfile.Rating
                    },
                    MaxTenants = l.maxTenants,
                    Reviews = l.RatingListListings.Select(rll => new
                    {
                        ReviewerFirstName = rll.Reviewer.UserProfile.FirstName,
                        ReviewerLastName = rll.Reviewer.UserProfile.LastName,
                        ReviewerPhotoUrl = rll.Reviewer.UserProfile.PhotoUrl,
                        ReviewerRating = rll.Reviewer.UserProfile.Rating,
                        rll.Description
                    }).ToList(),
                    l.Country,
                    l.Description,
                    l.PerWeek,
                    l.PerDay,
                    l.PerMonth // Добавляем поля для вычисления RentalTypes
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (listing == null)
                return NotFound("Listing not found");

            var rentalTypes = new List<string>
            {
                listing.PerWeek.HasValue ? "Аренда на неделю" : null,
                listing.PerDay.HasValue ? "Посуточная аренда" : null,
                listing.PerMonth.HasValue ? "Аренда на месяц" : null
            }.Where(rt => rt != null).ToList();

            var result = new
            {
                listing.HouseType,
                listing.Photos,
                listing.Title,
                listing.CheckInTime,
                listing.CheckOutTime,
                listing.Country,
                listing.Location,
                rentalTypes,
                listing.Price,
                listing.Amenities,
                listing.MainFeatures,
                listing.AverageRating,
                listing.Landlord,
                listing.MaxTenants,
                listing.Reviews,
                listing.Description
            };

            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateListing([FromBody] ListingModel model)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            var houseType = await _context.HouseTypes.FindAsync(model.HouseTypeId);
            if (houseType == null)
                return BadRequest("Invalid house type");

            var listing = new Listing
            {
                UserId = user.Id,
                HouseTypeId = model.HouseTypeId,
                Title = model.Title,
                CheckInTime = model.CheckInTime,
                CheckOutTime = model.CheckOurTime,
                Description = model.Description,
                PerWeek = model.PerWeek,
                PerDay = model.PerDay,
                PerMonth = model.PerMonth,
                Country = model.Country,
                Location = model.Location,
                Model3DUrl = model.Model3DUrl,
                IsModerated = false,
                CreatedDate = DateTime.UtcNow,
                maxTenants = model.maxTenants,
            };

            // Добавление удобств
            if (model.AmenityIds != null)
            {
                foreach (var amenityId in model.AmenityIds)
                {
                    var amenity = await _context.Amenities.FindAsync(amenityId);
                    if (amenity != null)
                    {
                        listing.ListingAmenities.Add(new ListingAmenity { AmenityId = amenityId, ListingId = listing.Id });
                    }
                }
            }

            // Добавление главных параметров
            if (model.MainFeatureIds != null)
            {
                foreach (var featureId in model.MainFeatureIds)
                {
                    var feature = await _context.MainFeatures.FindAsync(featureId);
                    if (feature != null)
                    {
                        listing.ListingMainFeatures.Add(new ListingMainFeature
                        {
                            MainFeatureId = featureId,
                            Value = feature.IsNumeric ? model.MainFeatureValues?[model.MainFeatureIds.IndexOf(featureId)] : null,
                            ListingId = listing.Id
                        });
                    }
                }
            }

            // Добавление фотографий
            if (model.PhotoUrls != null)
            {
                foreach (var photoUrl in model.PhotoUrls)
                {
                    var photo = new Photo { Url = photoUrl, CreatedDate = DateTime.UtcNow };
                    _context.Photos.Add(photo);
                    listing.ListingPhotos.Add(new ListingPhoto { Photo = photo, ListingId = listing.Id });
                }
            }

            _context.Listings.Add(listing);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Listing created successfully", listing.Id });
        }
        public class RentalType
        {
            public string? TypeName { get; set; } // ? позволяет null
            public bool IsAvailable { get; set; }
        }
        public class ListingModel
        {
            public int HouseTypeId { get; set; }
            public string Title { get; set; }
            public TimeSpan CheckInTime {  get; set; }
            public TimeSpan CheckOurTime { get; set; }
            public string Description { get; set; }
            public decimal? PerWeek { get; set; }
            public decimal? PerDay { get; set; }
            public decimal? PerMonth { get; set; }
            public string Location { get; set; }
            public string? Model3DUrl { get; set; } // Поле для 3D-модели
            public List<int> AmenityIds { get; set; } // Список ID удобств
            public List<int> MainFeatureIds { get; set; } // Список ID главных параметров
            public List<string> MainFeatureValues { get; set; } // Значения для числовых параметров
            public List<string> PhotoUrls { get; set; } // Список путей к фотографиям
            public int maxTenants {get; set; }
            public decimal? Rating {  get; set; }
            public string Country { get; set; }
        }
        public class RatingModel
        {
            public int ListingId { get; set; }
            public string Description { get; set; }
            public decimal Rating { get; set; }
        }
    }
}
