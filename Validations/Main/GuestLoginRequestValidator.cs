using FluentValidation;
using IPTS.Resources;
using IPTS.ViewModels;

namespace IPTS.Validators
{
    public class GuestLoginRequestValidator : AbstractValidator<GuestLoginRequest>
    {
        public GuestLoginRequestValidator(LocService localizer)
        {
            RuleFor(x => x.UsernameOrEmail)
                .NotEmpty().WithMessage(_ => localizer.GetSystem("UsernameOrEmailRequired"));
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(_ => localizer.GetSystem("PasswordRequired"));
        }
    }
}
