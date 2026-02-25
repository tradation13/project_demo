using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class VerifyEmailValidator : AbstractValidator<VerifyEmailViewModel>
    {
        public VerifyEmailValidator(LocService localizer)
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage(x => localizer.GetSystem("EmailRequired")).EmailAddress().WithMessage(x => localizer.GetSystem("InvalidEmailFormat"));
        }
    }
}
