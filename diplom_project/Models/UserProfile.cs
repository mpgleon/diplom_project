using System.ComponentModel.DataAnnotations;

namespace diplom_project.Models
{
    public class UserProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Surname { get; set; }
        public string? Location { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool IsVerified { get; set; } = false;
        public decimal? Rating { get; set; }
        public string? Description { get; set; }
        public string? PhotoUrl { get; set; }
        [Url]
        public string? Instagram { get; set; }
        [Url]
        public string? Facebook { get; set; }
        [Url]
        public string? Telegram { get; set; } 
        public User User { get; set; }
        public ICollection<UserProfileLanguage> UserProfileLanguages { get; set; }
        public string? Gender { get; set; }
        
    }
}
