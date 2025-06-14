namespace diplom_project.Models
{
    public class Amenity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<ListingAmenity> ListingAmenities { get; set; }
    }
}
