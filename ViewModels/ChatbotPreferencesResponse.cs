namespace IPTS.ViewModels
{
    public class ChatbotPreferencesResponse
    {
        public bool Success { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool AcceptedPrivacyPolicy { get; set; }
        public bool AcceptedTermsOfUse { get; set; }
        public bool ChatHistoryEnabled { get; set; }
        public string? Message { get; set; }
    }
}
