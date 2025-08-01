namespace diplom_project.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int? SenderId { get; set; } // Кто отправил
        public int RecipientId { get; set; } // Кому отправлено
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; } // Статус прочтения
        public User Sender { get; set; }
        public User Recipient { get; set; }
    }
}
