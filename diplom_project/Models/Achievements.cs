namespace diplom_project.Models
{
    public class Achievements
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Datestamp { get; set; }
        public string Description { get; set; }
        public decimal Commercial { get; set; }
    }
}
