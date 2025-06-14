namespace diplom_project.Models
{
    public class HouseType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<Listing> Listings { get; set; }
    }
}
