namespace diplom_project.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public string Password { get; set; }
        public DateTime Datestamp { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<UserRole> UserRoles { get; set; }
        public UserProfile? UserProfile { get; set; }
        public decimal Balance { get; set; }
    }
}
