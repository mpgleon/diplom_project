namespace diplom_project.Models
{
    public class RatingListUser
    {
        public int Id { get; set; } // Первичный ключ, IDENTITY
        public int UserId1 { get; set; } // Кто оставил отзыв
        public int UserId2 { get; set; } // Кому оставлен отзыв
        public string Description { get; set; }
        public decimal Rating { get; set; }
        public DateTime CreatedDate { get; set; }

        // Навигационные свойства
        public User Reviewer { get; set; } // Кто оставил отзыв
        public User ReviewedUser { get; set; } // Кому оставлен отзыв
    }
}
