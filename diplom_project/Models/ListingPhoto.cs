using System.ComponentModel.DataAnnotations;

namespace diplom_project.Models
{
    public class ListingPhoto
    {
        public int Id { get; set; }
        public int ListingId { get; set; }
        public int PhotoId { get; set; }
        public Listing Listing { get; set; }
        public Photo Photo { get; set; }
    }
}
