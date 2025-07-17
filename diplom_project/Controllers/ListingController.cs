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
    public class ListingController : ControllerBase
    {

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        public ListingController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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

        [HttpGet("selected/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSelectedListing(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.ListingPhotos)
                    .ThenInclude(lp => lp.Photo)
                .Include(l => l.User)
                    .ThenInclude(u => u.UserProfile)
                .Where(l => l.Id == id)
                .Select(l => new
                {
                    Photos = l.ListingPhotos.Select(lp => lp.Photo.Url).ToList(),
                    l.Title,
                    l.Country,
                    l.Location,
                    l.AverageRating,
                    Landlord = new
                    {
                        FullName = $"{l.User.UserProfile.FirstName} {l.User.UserProfile.LastName}",
                        l.User.UserProfile.PhotoUrl
                    }
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (listing == null)
                return NotFound("Listing not found");

            return Ok(listing);
        }

        [HttpGet("details/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetListingDetails(int id)
        {
            var listing = await _context.Listings
                .Where(l => l.Id == id)
                .Select(l => new
                {
                    CheckInTime = l.CheckInTime.ToString(@"hh\:mm"),
                    CheckOutTime = l.CheckOutTime.ToString(@"hh\:mm"),
                    MaxTenants = l.maxTenants,
                    Price = l.PerWeek ?? l.PerDay ?? l.PerMonth,
                    l.PerWeek,
                    l.PerDay,
                    l.PerMonth,
                    
                    
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();
            var rentalTypes = new List<string>
            {
                listing.PerWeek.HasValue ? "Аренда на неделю" : null,
                listing.PerDay.HasValue ? "Посуточная аренда" : null,
                listing.PerMonth.HasValue ? "Аренда на месяц" : null
            }.Where(rt => rt != null).ToList();
            if (listing == null)
                return NotFound("Listing not found");
            var result = new
            {
                listing.CheckInTime,
                listing.CheckOutTime, 
                listing.MaxTenants,
                rentalTypes,
                listing.Price
            };
            return Ok(result);
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
                    l.PerMonth
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

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            // Проверка роли Landlord с защитой от null
            if (!user.UserRoles.Any(ur => ur.Role.Name == "Landlord"))
                return BadRequest("Only users with Landlord role can create listings");

            var houseType = await _context.HouseTypes.FindAsync(model.HouseTypeId);
            if (houseType == null)
                return BadRequest("Invalid house type");

            var listing = new Listing
            {
                UserId = user.Id,
                HouseTypeId = model.HouseTypeId,
                Title = model.Title,
                CheckInTime = model.CheckInTime,
                CheckOutTime = model.CheckOutTime,
                Description = model.Description,
                PerWeek = model.PerWeek,
                PerDay = model.PerDay,
                PerMonth = model.PerMonth,
                Country = model.Country,
                Location = model.Location,
                Model3DUrl = model.Model3DUrl,
                IsModerated = false,
                CreatedDate = DateTime.UtcNow,
                maxTenants = model.maxTenants
                
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
            /*if (model.PhotoUrls != null)
            {
                foreach (var photoUrl in model.PhotoUrls)
                {
                    var photo = new Photo { Url = photoUrl, CreatedDate = DateTime.UtcNow };
                    _context.Photos.Add(photo);
                    listing.ListingPhotos.Add(new ListingPhoto { Photo = photo, ListingId = listing.Id });
                }
            }*/

            _context.Listings.Add(listing);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Listing created successfully", listing.Id });
        }

        [HttpPost("upload-photos")]
        [Authorize]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UploadPhotos([FromForm] int listingId, [FromForm] IFormFileCollection files)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            var listing = await _context.Listings
                .Include(l => l.ListingPhotos)
                .FirstOrDefaultAsync(l => l.Id == listingId && l.UserId == user.Id);
            if (listing == null)
                return NotFound("Listing not found or you don't have permission");

            if (files == null || !files.Any())
                return BadRequest("No files uploaded.");

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "listings", listingId.ToString());
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var photoUrls = new List<string>();
            foreach (var file in files)
            {
                if (!file.ContentType.StartsWith("image/"))
                    return BadRequest("Only image files are allowed.");

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Сохраняем относительный путь
                var photoUrl = $"listings\\{listingId}\\{fileName}";
                photoUrls.Add(photoUrl);

                // Добавляем запись в базу
                var photo = new Photo { Url = photoUrl, CreatedDate = DateTime.UtcNow };
                _context.Photos.Add(photo);
                listing.ListingPhotos.Add(new ListingPhoto { Photo = photo, ListingId = listingId });
            }

            await _context.SaveChangesAsync();

            return Ok(new { photoUrls });
        }
        [HttpGet("get-all-photos/{listingId}")]
        [AllowAnonymous] // Доступен без авторизации, так как фото публичны
        public async Task<IActionResult> GetAllPhotos(int listingId)
        {
            var listing = await _context.Listings
                .Include(l => l.ListingPhotos)
                .ThenInclude(lp => lp.Photo)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null)
                return NotFound("Listing not found");

            var photoUrls = listing.ListingPhotos
                .Select(lp => lp.Photo.Url)
                .ToList();

            return Ok(new { photoUrls });
        }
        [HttpGet("get-photo/{filePath}")]
        [AllowAnonymous]
        public IActionResult GetPhoto(string filePath)
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
        [HttpDelete("delete-photo")]
        [Authorize]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> DeletePhoto([FromQuery] int listingId, [FromQuery] string photoUrl)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            var listing = await _context.Listings
                .Include(l => l.ListingPhotos)
                .ThenInclude(lp => lp.Photo)
                .FirstOrDefaultAsync(l => l.Id == listingId && l.UserId == user.Id);
            if (listing == null)
                return NotFound("Listing not found or you don't have permission");

            // Преобразуем входящий photoUrl в формат базы данных (заменяем \ на %5C)
            //var dbPhotoUrl = photoUrl.Replace("\\", "%5C");

            // Находим фото по преобразованному URL
            var listingPhoto = listing.ListingPhotos
                .FirstOrDefault(lp => lp.Photo.Url == photoUrl);
            if (listingPhoto == null)
                return NotFound("Photo not found");

            // Преобразуем путь для файловой системы
            //var sanitizedPath = dbPhotoUrl.Replace("%5C", "\\");

            var fullPath = Path.Combine(_env.WebRootPath, "uploads", photoUrl);

            // Проверяем существование файла
            if (!System.IO.File.Exists(fullPath))
            {
                // Попробуем нормализовать путь
                var normalizedPath = Path.GetFullPath(fullPath);
                if (!System.IO.File.Exists(normalizedPath))
                {
                    return NotFound($"File not found at: {normalizedPath}");
                }
                fullPath = normalizedPath; // Используем нормализованный путь
            }

            try
            {
                // Проверяем права доступа
                var fileInfo = new FileInfo(fullPath);
                if (!fileInfo.Exists)
                    return NotFound($"File info not found at: {fullPath}");

                // Удаляем файл
                System.IO.File.Delete(fullPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, $"Access denied: {ex.Message}");
            }
            catch (IOException ex)
            {
                return StatusCode(500, $"IO error: {ex.Message}");
            }

            _context.ListingPhotos.Remove(listingPhoto);
            _context.Photos.Remove(listingPhoto.Photo);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Photo deleted successfully" });
        }
        [HttpPost("create-booking")]
        [Authorize]
        public async Task<IActionResult> CreateBooking([FromBody] BookingModel model)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            // Проверка соответствия ФИО, почты и телефона
            if (user.UserProfile == null ||
                model.FirstName != user.UserProfile.FirstName ||
                model.LastName != user.UserProfile.LastName ||
                model.Email != user.Email ||
                model.Phone != user.Phone)
            {
                return BadRequest("Provided personal details do not match the current user or profile is incomplete");
            }

            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == model.ListingId);
            if (listing == null)
                return NotFound("Listing not found");

            // Проверка isModerated и роли Admin
            if (listing.IsModerated == false || listing.IsModerated == null)
                return BadRequest("Listing must be moderated by an Admin to create a booking");

            if(listing.isOccupied == true)
                return BadRequest("Listing already booked");

            // Проверка CheckInTime
            if (model.CheckInTime < listing.CheckInTime)
                return BadRequest($"Check-in time must be at least {listing.CheckInTime:hh\\:mm}");

            // Проверка количества людей
            if (model.NumberOfPeople > listing.maxTenants)
                return BadRequest($"Number of people ({model.NumberOfPeople}) exceeds maximum allowed ({listing.maxTenants})");

            // Расчет количества дней, недель или месяцев
            var dateFrom = model.DateFrom.Date;
            var dateTo = model.DateTo.Date;
            if (dateFrom > dateTo)
                return BadRequest("DateFrom cannot be later than DateTo");

            var totalDays = (dateTo - dateFrom).Days + 1;
            decimal totalPrice = 0;

            if (listing.PerDay.HasValue && totalDays <= 7)
            {
                totalPrice = listing.PerDay.Value * totalDays;
            }
            else if (listing.PerWeek.HasValue && totalDays >= 7 && totalDays <= 30)
            {
                var totalWeeks = (int)Math.Ceiling(totalDays / 7.0);
                totalPrice = listing.PerWeek.Value * totalWeeks;
            }
            else if (listing.PerMonth.HasValue && totalDays > 30)
            {
                var totalMonths = (int)Math.Ceiling(totalDays / 30.0);
                totalPrice = listing.PerMonth.Value * totalMonths;
            }
            else
            {
                return BadRequest("No valid pricing option available for the selected period");
            }

            // Проверка баланса
            if (user.Balance < totalPrice)
                return BadRequest($"Insufficient balance. Required: {totalPrice}, Available: {user.Balance}");

            // Создание записи в PendingListings
            var pendingListing = new PendingListing
            {
                UserId = user.Id,
                ListingId = model.ListingId,
                Description = model.Description,
                DateFrom = dateFrom,
                DateTo = dateTo,
                CheckInTime = model.CheckInTime,
                Confirmed = false,
                NumberOfPeople = model.NumberOfPeople,
                TotalPrice = totalPrice
            };

            _context.PendingListings.Add(pendingListing);


            // Перевод суммы на PendingBalance
            user.Balance -= totalPrice;
            user.PendingBalance += totalPrice;
            _context.Users.Update(user);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Booking request created successfully",
                pendingListingId = pendingListing.Id,
                totalPrice = totalPrice
            });
        }

        // Метод для отмены бронирования
        [HttpPost("cancel-booking/{pendingListingId}")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(int pendingListingId)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            var pendingListing = await _context.PendingListings
                .Include(pl => pl.Listing) // Загружаем связанный Listing
                .FirstOrDefaultAsync(pl => pl.Id == pendingListingId);
            if (pendingListing == null)
                return NotFound("Booking request not found");

            // Проверка прав: либо создатель заявки, либо владелец Listing
            if (pendingListing.UserId != user.Id && pendingListing.Listing.UserId != user.Id)
                return Unauthorized(new { message = "Only the tenant or listing owner can cancel the booking" });

            if (pendingListing.Confirmed)
                return BadRequest("Cannot cancel a confirmed booking");

            // Возврат суммы на Balance
            var requester = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == pendingListing.UserId);
            if (requester == null)
                return NotFound("Requester not found");

            requester.Balance += pendingListing.TotalPrice;
            requester.PendingBalance -= pendingListing.TotalPrice;
            _context.PendingListings.Remove(pendingListing);

            await _context.SaveChangesAsync();

            // Сообщение зависит от роли
            var message = pendingListing.UserId == user.Id
                ? "canceled by tenant"
                : "canceled by landlord";

            return Ok(new { message = $"Booking {message} successfully" });
        }
        [HttpPost("moderate-listing/{listingId}")]
        [Authorize]
        public async Task<IActionResult> ModerateListing(int listingId)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            // Проверка роли Admin
            if (!user.UserRoles.Any(ur => ur.Role.Name == "Admin"))
                return BadRequest("Only Admin can moderate listings");

            var listing = await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == listingId);
            if (listing == null)
                return NotFound("Listing not found");

            // Установка isModerated = true
            if (listing.IsModerated == true)
                return BadRequest("Listing is already moderated");

            listing.IsModerated = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Listing moderated successfully" });
        }
        // Метод для подтверждения бронирования
        [HttpPost("confirm-booking/{pendingListingId}")]
        [Authorize]
        public async Task<IActionResult> ConfirmBooking(int pendingListingId)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            var pendingListing = await _context.PendingListings
                .Include(pl => pl.Listing) // Загружаем связанный Listing
                .FirstOrDefaultAsync(pl => pl.Id == pendingListingId);
            if (pendingListing == null)
                return NotFound("Booking request not found");

            // Проверка, что текущий пользователь — владелец Listing
            if (pendingListing.Listing.UserId != user.Id)
            {
                return Unauthorized(new { message = "Only the listing owner can confirm the booking" });
            }
            if (pendingListing.Confirmed)
                return BadRequest("Booking is already confirmed");

            // Получаем все остальные заявки на этот listing
            var otherPendingListings = await _context.PendingListings
                .Where(pl => pl.ListingId == pendingListing.ListingId && pl.Id != pendingListingId)
                .ToListAsync();

            foreach (var otherPending in otherPendingListings)
            {
                var requester3 = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == otherPending.UserId);

                if (requester3 != null)
                {
                    // Возврат суммы на Balance
                    requester3.Balance += otherPending.TotalPrice;
                    requester3.PendingBalance -= otherPending.TotalPrice;
                    _context.Users.Update(requester3);
                }
                _context.PendingListings.Remove(otherPending);
            }

            var requester = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == pendingListing.UserId);
            if (requester == null)
                return NotFound("Requester not found");

            // Подтверждение бронирования
            pendingListing.Confirmed = true;
            pendingListing.Listing.isOccupied = true;
            // Перевод денег с PendingBalance отправителя на Balance владельца
            requester.PendingBalance -= pendingListing.TotalPrice;
            user.Balance += pendingListing.TotalPrice; // Прибавляем владельцу

            await _context.SaveChangesAsync();

            return Ok(new { message = "Booking confirmed successfully" });
        }
        [HttpDelete("delete-listing/{listingId}")]
        [Authorize]
        public async Task<IActionResult> DeleteListing(int listingId)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            var listing = await _context.Listings
                .Include(l => l.ListingPhotos)
                .ThenInclude(lp => lp.Photo)
                .FirstOrDefaultAsync(l => l.Id == listingId);
            if (listing == null)
                return NotFound("Listing not found");

            // Проверка, что текущий пользователь — создатель объявления
            if (listing.UserId != user.Id)
                return Forbid("Only the creator of the listing can delete it");

            // Удаление связанных фотографий с диска
            foreach (var listingPhoto in listing.ListingPhotos)
            {
                var fullPath = Path.Combine(_env.WebRootPath, "uploads", listingPhoto.Photo.Url);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            // Удаление записи Listings (связанные данные удалятся каскадно)
            _context.Listings.Remove(listing);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Listing deleted successfully" });
        }
        [HttpGet("user-trips/{userId}")]
        public async Task<IActionResult> GetUserTrips(int userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found");

            var trips = await _context.PendingListings
                .Include(pl => pl.Listing)
                    .ThenInclude(l => l.ListingPhotos)
                    .ThenInclude(lp => lp.Photo)
                .Where(pl => pl.UserId == user.Id && pl.Confirmed)
                .Select(pl => new
                {
                    DateRange = $"{pl.DateFrom:dd.MM}-{pl.DateTo:dd.MM}",
                    PhotoUrl = pl.Listing.ListingPhotos
                        .OrderBy(lp => lp.PhotoId)
                        .Select(lp => lp.Photo.Url)
                        .FirstOrDefault(),
                    Rating = pl.Listing.AverageRating ?? 0,
                    Country = pl.Listing.Country,
                    Location = pl.Listing.Location
                })
                .ToListAsync();

            return Ok(trips);
        }
        [HttpGet("get-landlord-bookingsDate")]
        [Authorize]
        public async Task<IActionResult> GetLandlordBookings()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            // Проверка роли Landlord
            if (!user.UserRoles.Any(ur => ur.Role.Name == "Landlord"))
                return Forbid("Only Landlord can view their bookings");

            // Получаем все Listings пользователя с их PendingListings
            var bookings = await (from l in _context.Listings
                                  join pl in _context.PendingListings on l.Id equals pl.ListingId
                                  where l.UserId == user.Id
                                  select new
                                  {
                                      ListingId = pl.ListingId,
                                      DateFrom = pl.DateFrom,
                                      DateTo = pl.DateTo,
                                      ListingName = l.Title
                                  }).ToListAsync();

            return Ok(bookings);
        }
        [HttpGet("get-landlord-listings")]
        [Authorize]
        public async Task<IActionResult> GetLandlordListings()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound("User not found");

            // Проверка роли Landlord
            if (!user.UserRoles.Any(ur => ur.Role.Name == "Landlord"))
                return Forbid("Only Landlord can view their listings");

            // Получаем все Listings пользователя с необходимыми данными
            var listings = await _context.Listings
                .Where(l => l.UserId == user.Id)
                .Select(l => new
                {
                    Id = l.Id,
                    Title = l.Title,
                    Country = l.Country,
                    IsModerated = l.IsModerated,
                    IsOccupied = l.isOccupied,
                    AverageRating = l.AverageRating,
                    PhotoUrl = l.ListingPhotos
                        .OrderBy(lp => lp.PhotoId) // Берем первую фотографию по Id
                        .Select(lp => lp.Photo.Url)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(listings);
        }
        public class RentalType
        {
            public string? TypeName { get; set; }
            public bool IsAvailable { get; set; }
        }
        public class ListingModel
        {
            public int Id {  get; set; }
            public int HouseTypeId { get; set; }
            public string Title { get; set; }
            public TimeSpan CheckInTime {  get; set; }
            public TimeSpan CheckOutTime { get; set; }
            public string Description { get; set; }
            public decimal? PerWeek { get; set; }
            public decimal? PerDay { get; set; }
            public decimal? PerMonth { get; set; }
            public string Location { get; set; }
            public string? Model3DUrl { get; set; } // Поле для 3D-модели
            public List<int> AmenityIds { get; set; } // Список ID удобств
            public List<int> MainFeatureIds { get; set; } // Список ID главных параметров
            public List<string> MainFeatureValues { get; set; } // Значения для числовых параметров
            //public List<string> PhotoUrls { get; set; } // Список путей к фотографиям
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
