using diplom_project.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace diplom_project.Services
{
    public class BookingExpirationService : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _timer;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Уменьшен для тестирования, верните 5 минут после тестов
        private bool _isRunning;

        public BookingExpirationService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("BookingExpirationService starting at " + DateTime.UtcNow);
            _timer = new Timer(CheckAndUpdateBookings, null, TimeSpan.Zero, _checkInterval);
            _isRunning = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("BookingExpirationService stopping at " + DateTime.UtcNow);
            _isRunning = false;
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async void CheckAndUpdateBookings(object state)
        {
            if (!_isRunning) return;
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var currentDateTime = DateTime.UtcNow;
                    Console.WriteLine($"Checking bookings at {currentDateTime}");

                    // Проверяем истекшие бронирования по отдельным полям DateTo и CheckOutTime
                    var expiredBookings = await context.PendingListings
                        .Include(pl => pl.Listing)
                        .Where(pl => pl.Confirmed && !pl.Expired && pl.Listing != null)
                        .Where(pl =>
                            pl.DateTo < currentDateTime.Date ||
                            (pl.DateTo == currentDateTime.Date && pl.CheckOutTime <= currentDateTime.TimeOfDay))
                        .ToListAsync();

                    Console.WriteLine($"Found {expiredBookings.Count} expired bookings");
                    if (!expiredBookings.Any())
                        return;

                    foreach (var booking in expiredBookings)
                    {
                        Console.WriteLine($"Processing booking {booking.Id}, DateTo: {booking.DateTo}, CheckOutTime: {booking.CheckOutTime}");
                        booking.Expired = true;
                        if (booking.Listing != null)
                        {
                            booking.Listing.isOccupied = false;
                            context.Listings.Update(booking.Listing);
                            Console.WriteLine($"Updated listing {booking.ListingId} to isOccupied = false");
                        }
                        context.PendingListings.Update(booking);
                    }

                    await context.SaveChangesAsync();
                    Console.WriteLine($"Saved changes for {expiredBookings.Count} bookings");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BookingExpirationService: {ex.Message}");
            }
        }
    }
}
