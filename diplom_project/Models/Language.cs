namespace diplom_project.Models
{
    public class Language
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }

        // Связь с UserProfileLanguages (многие-ко-многим)
        public ICollection<UserProfileLanguage> UserProfileLanguages { get; set; }
    }
}
