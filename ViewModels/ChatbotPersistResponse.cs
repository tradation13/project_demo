namespace IPTS.ViewModels
{
    public class ChatbotPersistResponse
    {
        public bool Success { get; set; }
        public bool Saved { get; set; }
        public bool SkippedDueToConsent { get; set; }
        public bool SkippedDueToInvalidInput { get; set; }
        public bool SkippedDueToIntegrity { get; set; }
        public bool SkippedDueToIdentityMismatch { get; set; }
        public int? ConversationId { get; set; }
        public int? MessageId { get; set; }
        public string? Message { get; set; }
    }
}
