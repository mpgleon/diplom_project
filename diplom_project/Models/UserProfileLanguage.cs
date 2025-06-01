namespace diplom_project.Models
{
    public class UserProfileLanguage
    {
        public int UserProfileId { get; set; }
        public UserProfile UserProfile { get; set; }

        public int LanguageId { get; set; }
        public Language Language { get; set; }
    }
}
