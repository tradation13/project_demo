using FluentValidation;
using IPTS.ViewModels;

namespace IPTS.Validators
{
    public class ChatbotConsentRequestValidator : AbstractValidator<ChatbotConsentRequest>
    {
        public ChatbotConsentRequestValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty()
                .MaximumLength(64);
        }
    }
}
