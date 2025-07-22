using System.ComponentModel.DataAnnotations.Schema;

namespace diplom_project.Models
{
    public class PendingListing
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        [ForeignKey("Listing")]
        public int ListingId { get; set; }
        public Listing Listing { get; set; }
        public bool Expired { get; set; }
        public string Description { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public TimeSpan CheckInTime { get; set; }
        public TimeSpan CheckOutTime { get; set; }
        public bool Confirmed { get; set; }
        public int NumberOfPeople { get; set; }
        public decimal TotalPrice { get; set; }
        public User User { get; set; }
    }
}
