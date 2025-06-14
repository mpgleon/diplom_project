using System.ComponentModel.DataAnnotations;

namespace diplom_project.Models
{
    public class ListingAmenity
    {
        public int ListingId { get; set; }
        public int AmenityId { get; set; }
        public Listing Listing { get; set; }
        public Amenity Amenity { get; set; }
    }
}
