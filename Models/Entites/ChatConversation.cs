using IPTS.Models.Enums;

namespace IPTS.Models.Entites
{
    public class ChatConversation
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public ChatbotUserType UserType { get; set; } = ChatbotUserType.Guest;
        public string? IpAddress { get; set; }
        public bool ConsentGiven { get; set; }
        public DateTime? ConsentDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
