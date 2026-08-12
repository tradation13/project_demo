using FluentValidation;
using IPTS.ViewModels;

namespace IPTS.Validators
{
    public class ChatbotChatHistoryPreferenceRequestValidator : AbstractValidator<ChatbotChatHistoryPreferenceRequest>
    {
        public ChatbotChatHistoryPreferenceRequestValidator()
        {
            // Enabled is bool — always present. SessionId optional; when provided, length-capped.
            RuleFor(x => x.SessionId)
                .MaximumLength(64)
                .When(x => !string.IsNullOrWhiteSpace(x.SessionId));
        }
    }
}
