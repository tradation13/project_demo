using FluentValidation;
using IPTS.Resources;
using IPTS.ViewModels;

namespace IPTS.Validators
{
    public class GuestResendConfirmationRequestValidator : AbstractValidator<GuestResendConfirmationRequest>
    {
        public GuestResendConfirmationRequestValidator(LocService localizer)
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(_ => localizer.GetSystem("EmailRequired"))
                .EmailAddress().WithMessage(_ => localizer.GetSystem("InvalidEmailFormat"));
        }
    }
}
