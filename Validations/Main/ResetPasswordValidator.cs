using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordViewModel>
    {
        public ResetPasswordValidator(LocService localizer)
        {
            RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage(x => localizer.GetSystem("PasswordRequired"));
            RuleFor(x => x.NewPassword).NotEmpty().WithMessage(x => localizer.GetSystem("PasswordRequired")).MinimumLength(6).WithMessage(x => localizer.GetSystem("PasswordMinLength"));
            RuleFor(x => x.ConfirmPassword).NotEmpty().WithMessage(x => localizer.GetSystem("PasswordRequired")).Equal(x => x.NewPassword).WithMessage(x => localizer.GetSystem("PasswordsDoNotMatch"));
        }
    }
}
