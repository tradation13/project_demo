using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class ResetPasswordConfirmValidator : AbstractValidator<ResetPasswordConfirmViewModel>
    {
        public ResetPasswordConfirmValidator(LocService localizer)
        {
            RuleFor(x => x.Token).NotEmpty().WithMessage(x => localizer.GetSystem("TokenRequired"));
            RuleFor(x => x.Email).NotEmpty().WithMessage(x => localizer.GetSystem("EmailRequired")).EmailAddress().WithMessage(x => localizer.GetSystem("InvalidEmailFormat"));
            RuleFor(x => x.NewPassword).NotEmpty().WithMessage(x => localizer.GetSystem("PasswordRequired")).MinimumLength(6).WithMessage(x => localizer.GetSystem("PasswordMinLength"));
            RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword).WithMessage(x => localizer.GetSystem("PasswordsDoNotMatch"));
        }
    }
}
