namespace diplom_project.Models
{
    public class UserProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool IsVerified { get; set; } = false;
        public decimal Rating { get; set; } = 0;
        public string? Description { get; set; }
        public string? PhotoUrl { get; set; }
        public ICollection<UserProfileLanguage> UserProfileLanguages { get; set; }
    }
}
