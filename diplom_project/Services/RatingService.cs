using Microsoft.EntityFrameworkCore;

namespace diplom_project.Services
{
    public class RatingService : IRatingService
    {
        private readonly AppDbContext _context;

        public RatingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task UpdateUserRatingAsync(int userId)
        {
            var ratings = await _context.RatingListUsers
                .Where(rlu => rlu.UserId2 == userId)
                .Select(rlu => rlu.Rating)
                .ToListAsync();

            // Получаем UserProfile напрямую, избегая анонимных типов
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(up => up.UserId == userId);

            if (userProfile == null)
            {
                return; // Пользователь не найден, выходим
            }

            if (ratings.Any())
            {
                userProfile.Rating = ratings.Average();
            }
            else
            {
                userProfile.Rating = 0.0m; // Устанавливаем 0 при отсутствии отзывов
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateListingRatingAsync(int listingId)
        {
            var ratings = await _context.RatingListListings
                .Where(rll => rll.ListingId == listingId)
                .Select(rll => rll.Rating)
                .ToListAsync();

            if (ratings.Any())
            {
                var averageRating = ratings.Average();
                var listing = await _context.Listings
                    .FirstOrDefaultAsync(l => l.Id == listingId);
                if (listing != null)
                {
                    listing.AverageRating = averageRating; // Добавим это поле
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
