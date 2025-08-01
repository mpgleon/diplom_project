namespace diplom_project.Models
{
    public class RatingListListing
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ListingId { get; set; }
        public string Description { get; set; }
        public decimal Rating { get; set; }
        public DateTime CreatedDate { get; set; }
        public User Reviewer { get; set; }
        public Listing Listing { get; set; }
    }
}
