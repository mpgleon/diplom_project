namespace diplom_project.Models
{
    public class MainFeature
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsNumeric { get; set; }
        public ICollection<ListingMainFeature> ListingMainFeatures { get; set; }
    }
}
