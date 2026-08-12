using FluentValidation;
using IPTS.ViewModels;

namespace IPTS.Validators
{
    public class ChatbotMessageRequestValidator : AbstractValidator<ChatbotMessageRequest>
    {
        public ChatbotMessageRequestValidator()
        {
            RuleFor(x => x.SessionId)
                .NotEmpty()
                .MaximumLength(64);

            RuleFor(x => x.Message)
                .NotEmpty()
                .MaximumLength(4000);
        }
    }
}
