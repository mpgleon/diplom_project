using Microsoft.EntityFrameworkCore;
using diplom_project.Models;

namespace diplom_project
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        }
}
