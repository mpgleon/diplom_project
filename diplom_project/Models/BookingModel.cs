using System.ComponentModel.DataAnnotations;

namespace diplom_project.Models
{
    public class BookingModel
    {
        [Required]
        public int ListingId { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [Phone]
        public string Phone { get; set; }
        public string Description { get; set; }
        [Required]
        public DateTime DateFrom { get; set; }
        [Required]
        public DateTime DateTo { get; set; }
        [Required]
        public TimeSpan CheckInTime { get; set; }
        public TimeSpan CheckOutTime { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Number of people must be positive")]
        public int NumberOfPeople { get; set; }
    }
}
