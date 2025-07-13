using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using diplom_project.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace diplom_project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ChatController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet("last-messages")]
        [Authorize]
        public async Task<IActionResult> GetAllSentMessageInfo()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var sender = await _context.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email == email);
            if (sender == null || sender.UserProfile == null)
                return NotFound("Sender not found");

            var sentMessages = await _context.ChatMessages
                .Where(cm => cm.SenderId == sender.Id)
                .GroupBy(cm => cm.RecipientId)
                .Select(g => new
                {
                    RecipientId = g.Key,
                    LastMessage = g.OrderByDescending(cm => cm.Timestamp).Select(cm => new { cm.Message, cm.Timestamp }).FirstOrDefault(),
                    Recipient = _context.Users
                        .Include(u => u.UserProfile)
                        .Where(u => u.Id == g.Key)
                        .Select(u => new
                        {
                            u.Id,
                            Avatar = u.UserProfile.PhotoUrl,
                            FullName = $"{u.UserProfile.FirstName} {u.UserProfile.LastName}"
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            var response = sentMessages.Select(sm => new
            {
                RecipientId = sm.Recipient.Id,
                Avatar = sm.Recipient.Avatar,
                FullName = sm.Recipient.FullName,
                LastMessage = sm.LastMessage?.Message ?? "No messages yet",
                LastMessageTime = sm.LastMessage?.Timestamp.ToString("yyyy-MM-dd HH:mm") ?? ""
            });

            return Ok(response);
        }
        [HttpPost("send")]
        [Authorize]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageModel model)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (sender == null)
                return NotFound("Sender not found");

            var recipient = await _context.Users.FindAsync(model.RecipientId);
            if (recipient == null)
                return NotFound("Recipient not found");

            var message = new ChatMessage
            {
                SenderId = sender.Id,
                RecipientId = model.RecipientId,
                Message = model.Message,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new { messageId = message.Id });
        }

        [HttpGet("messages/{recipientId}")]
        [Authorize]
        public async Task<IActionResult> GetMessages(int recipientId)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (sender == null)
                return NotFound("Sender not found");

            var messages = await _context.ChatMessages
                .Where(cm => (cm.SenderId == sender.Id && cm.RecipientId == recipientId) ||
                            (cm.SenderId == recipientId && cm.RecipientId == sender.Id))
                .Select(cm => new
                {
                    cm.Id,
                    SenderName = cm.Sender.Email,
                    cm.Message,
                    cm.Timestamp,
                    cm.IsRead
                })
                .ToListAsync();

            // Отмечаем непрочитанные сообщения как прочитанные
            var unreadMessages = await _context.ChatMessages
                .Where(cm => cm.RecipientId == sender.Id && cm.SenderId == recipientId && !cm.IsRead)
                .ToListAsync();
            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            return Ok(messages);
        }
    }

    public class ChatMessageModel
    {
        public int RecipientId { get; set; }
        public string Message { get; set; }
    }
}