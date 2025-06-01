using Microsoft.EntityFrameworkCore;
using diplom_project.Models;

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
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
        }
    }
}
