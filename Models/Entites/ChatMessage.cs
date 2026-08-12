using IPTS.Models.Enums;

namespace IPTS.Models.Entites
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ChatConversationId { get; set; }
        public ChatConversation ChatConversation { get; set; } = null!;
        public ChatbotMessageSender SenderType { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
