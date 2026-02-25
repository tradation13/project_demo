using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class LoginValidator : AbstractValidator<LoginViewModel>
    {
        public LoginValidator(LocService localizer)
        {
            RuleFor(x => x.UsernameOrEmail).NotEmpty().WithMessage(x => localizer.GetSystem("UsernameOrEmailRequired"));
            RuleFor(x => x.Password).NotEmpty().WithMessage(x => localizer.GetSystem("PasswordRequired"));
        }
    }
}
