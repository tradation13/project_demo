namespace IPTS.ViewModels
{
    public class ChatbotChatHistoryPreferenceRequest
    {
        public bool Enabled { get; set; }
        /// <summary>Optional current browser session to GrantConsent when enabling.</summary>
        public string? SessionId { get; set; }
    }
}
