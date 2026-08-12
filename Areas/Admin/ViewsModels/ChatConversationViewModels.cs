using IPTS.Models.Enums;

namespace IPTS.Areas.Admin.ViewsModels
{
    public class ChatConversationListViewModel
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public List<ChatConversationListItemViewModel> Items { get; set; } = new();
    }

    public class ChatConversationListItemViewModel
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public ChatbotUserType UserType { get; set; }
        public string? IpAddress { get; set; }
        public bool ConsentGiven { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastMessageAt { get; set; }
    }

    public class ChatConversationDetailsViewModel
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public ChatbotUserType UserType { get; set; }
        public string? IpAddress { get; set; }
        public bool ConsentGiven { get; set; }
        public DateTime? ConsentDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastMessageAt { get; set; }
        public List<ChatMessageItemViewModel> Messages { get; set; } = new();
    }

    public class ChatMessageItemViewModel
    {
        public int Id { get; set; }
        public ChatbotMessageSender SenderType { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
