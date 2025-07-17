using Microsoft.EntityFrameworkCore;
using diplom_project.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace diplom_project
{

    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<UserProfileLanguage> UserProfileLanguages { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<HouseType> HouseTypes { get; set; }
        public DbSet<Amenity> Amenities { get; set; }
        public DbSet<ListingAmenity> ListingAmenities { get; set; }
        public DbSet<MainFeature> MainFeatures { get; set; }
        public DbSet<ListingMainFeature> ListingMainFeatures { get; set; }
        public DbSet<Photo> Photos { get; set; } 
        public DbSet<ListingPhoto> ListingPhotos { get; set; } 
        public DbSet<RatingListUser> RatingListUsers { get; set; }
        public DbSet<RatingListListing> RatingListListings { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<PendingListing> PendingListings { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PendingListing>()
                .HasOne(pl => pl.Listing)
                .WithMany()
                .HasForeignKey(pl => pl.ListingId);

            modelBuilder.Entity<ChatMessage>()
                .HasKey(cm => cm.Id);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Sender)
                .WithMany()
                .HasForeignKey(cm => cm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Recipient)
                .WithMany()
                .HasForeignKey(cm => cm.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Уникальный индекс для пары SenderId-RecipientId
            modelBuilder.Entity<ChatMessage>()
                .HasIndex(cm => new { cm.SenderId, cm.RecipientId })
                .IsUnique(false);

            modelBuilder.Entity<Listing>()
                .Property(l => l.CheckInTime)
                .HasColumnType("time");

            modelBuilder.Entity<Listing>()
                .Property(l => l.CheckOutTime)
                .HasColumnType("time");

            // Конфигурация для RatingListListing
            modelBuilder.Entity<RatingListListing>()
                .HasKey(rll => rll.Id);

            modelBuilder.Entity<RatingListListing>()
                .HasOne(rll => rll.Reviewer)
                .WithMany()
                .HasForeignKey(rll => rll.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RatingListListing>()
                .HasOne(rll => rll.Listing)
                .WithMany(l => l.RatingListListings)
                .HasForeignKey(rll => rll.ListingId) // Явно указываем ListingId
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RatingListListing>()
                .Property(rll => rll.ListingId)
                .HasColumnName("ListingId"); // Убеждаемся, что имя столбца совпадает

            modelBuilder.Entity<RatingListListing>()
                .HasIndex(rll => new { rll.UserId, rll.ListingId }) // Уникальный индекс
                .IsUnique(); // Запрет дублирования отзывов

            // Конфигурация для RatingListUser
            modelBuilder.Entity<RatingListUser>()
                .HasKey(rlu => rlu.Id); // Id как первичный ключ

            modelBuilder.Entity<RatingListUser>()
                .HasOne(rlu => rlu.Reviewer)
                .WithMany() // Нет обратной навигации
                .HasForeignKey(rlu => rlu.UserId1);


            modelBuilder.Entity<RatingListUser>()
                .HasOne(rlu => rlu.ReviewedUser)
                .WithMany() // Нет обратной навигации
                .HasForeignKey(rlu => rlu.UserId2);


            modelBuilder.Entity<RatingListUser>()
                .HasIndex(rlu => new { rlu.UserId1, rlu.UserId2 }) // Уникальный индекс для пары
                .IsUnique(); // Запрет дублирования отзывов

            modelBuilder.Entity<ListingAmenity>()
                .HasKey(la => new { la.ListingId, la.AmenityId });

            modelBuilder.Entity<ListingMainFeature>()
                .HasKey(lmf => new { lmf.ListingId, lmf.MainFeatureId });

            modelBuilder.Entity<ListingPhoto>()
                .HasKey(lp => new { lp.ListingId, lp.PhotoId });

            modelBuilder.Entity<Listing>()
                .HasMany(l => l.ListingAmenities)
                .WithOne(la => la.Listing)
                .HasForeignKey(la => la.ListingId);

            modelBuilder.Entity<Listing>()
                .HasMany(l => l.ListingMainFeatures)
                .WithOne(lmf => lmf.Listing)
                .HasForeignKey(lmf => lmf.ListingId);

            modelBuilder.Entity<Listing>()
                .HasMany(l => l.ListingPhotos)
                .WithOne(lp => lp.Listing)
                .HasForeignKey(lp => lp.ListingId);

            // Настройка UserRoles
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // Настройка UserProfile
            modelBuilder.Entity<UserProfile>()
                .HasOne(up => up.User)
                .WithOne(u => u.UserProfile)
                .HasForeignKey<UserProfile>(up => up.UserId);

            // Настройка UserProfileLanguages
            modelBuilder.Entity<UserProfileLanguage>()
                .HasKey(upl => new { upl.UserProfileId, upl.LanguageId });

            modelBuilder.Entity<UserProfileLanguage>()
                .HasOne(upl => upl.UserProfile)
                .WithMany(up => up.UserProfileLanguages)
                .HasForeignKey(upl => upl.UserProfileId);

            modelBuilder.Entity<UserProfileLanguage>()
                .HasOne(upl => upl.Language)
                .WithMany(l => l.UserProfileLanguages)
                .HasForeignKey(upl => upl.LanguageId);

            // Начальные данные для Languages
            modelBuilder.Entity<Language>().HasData(
                new Language { Id = 1, Code = "EN", Name = "English" },
                new Language { Id = 2, Code = "RU", Name = "Русский" },
                new Language { Id = 3, Code = "FR", Name = "Français" },
                new Language { Id = 4, Code = "DE", Name = "Deutsch" },
                new Language { Id = 5, Code = "UA", Name = "Ukrainian"}
            );
            modelBuilder.Entity<Listing>()
                .Property(l => l.AverageRating)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Listing>()
                .Property(l => l.PerDay)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Listing>()
                .Property(l => l.PerMonth)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Listing>()
                .Property(l => l.PerWeek)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RatingListListing>()
                .Property(rll => rll.Rating)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<UserProfile>()
                .Property(up => up.Rating)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<User>()
                .Property(u => u.Balance)
                .HasColumnType("decimal(18,2)");
        }
    }
}
