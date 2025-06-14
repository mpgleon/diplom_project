﻿using diplom_project.Models;
using diplom_project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace diplom_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListingController : ControllerBase
    {

        private readonly AppDbContext _context;
        private readonly IAuthService _authService;

        public ListingController(AppDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
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
                Description = model.Description,
                PerHour = model.PerHour,
                PerDay = model.PerDay,
                PerMonth = model.PerMonth,
                Location = model.Location,
                Model3DUrl = model.Model3DUrl,
                IsModerated = false,
                CreatedDate = DateTime.UtcNow
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

            return Ok(new { message = "Listing created successfully", id = listing.Id });
        }
        public class ListingModel
        {
            public int HouseTypeId { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public decimal? PerHour { get; set; }
            public decimal? PerDay { get; set; }
            public decimal? PerMonth { get; set; }
            public string Location { get; set; }
            public string? Model3DUrl { get; set; } // Поле для 3D-модели
            public List<int> AmenityIds { get; set; } // Список ID удобств
            public List<int> MainFeatureIds { get; set; } // Список ID главных параметров
            public List<string> MainFeatureValues { get; set; } // Значения для числовых параметров
            public List<string> PhotoUrls { get; set; } // Список путей к фотографиям
        }
    }
}
