namespace diplom_project.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Связь с UserRoles (многие-ко-многим)
        public ICollection<UserRole> UserRoles { get; set; }
    }
}
