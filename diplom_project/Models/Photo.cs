namespace diplom_project.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Description { get; set; }
        public ICollection<ListingPhoto> ListingPhotos { get; set; }
    }
}
