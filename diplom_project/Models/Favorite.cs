namespace diplom_project.Models
{
    public class Favorite
    {
        public int id { get; set; }
        public int idUser { get; set; }
        public int idListing { get; set; }
        public Listing Listing { get; set; }
        public User User { get; set; }
    }
}
