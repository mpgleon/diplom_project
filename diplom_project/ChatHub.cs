namespace diplom_project;
using Microsoft.AspNetCore.SignalR;
using diplom_project.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class ChatHub : Hub
{
    private readonly AppDbContext _context;

    public ChatHub(AppDbContext context)
    {
        _context = context;
    }

    public async Task SendMessage(int recipientId, string message, int? listingId = null)
    {
        var email = Context.User?.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email)) return;

        var sender = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (sender == null) return;

        var recipient = await _context.Users.FindAsync(recipientId);
        if (recipient == null) return;

        var chatMessage = new ChatMessage
        {
            SenderId = sender.Id,
            RecipientId = recipientId,
            Message = message,
            Timestamp = DateTime.UtcNow,
            IsRead = false
        };

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        // Отправка сообщения всем подключенным клиентам (или только recipient)
        await Clients.User(recipientId.ToString()).SendAsync("ReceiveMessage", sender.Id, message, chatMessage.Timestamp);
    }
}