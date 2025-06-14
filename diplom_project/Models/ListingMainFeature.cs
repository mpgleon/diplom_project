using System.ComponentModel.DataAnnotations;

namespace diplom_project.Models
{
    public class ListingMainFeature
    {
        public int ListingId { get; set; }
        public int MainFeatureId { get; set; }
        public string Value { get; set; } // Числовое значение для параметров вроде спален или площади
        public Listing Listing { get; set; }
        public MainFeature MainFeature { get; set; }
    }
}
