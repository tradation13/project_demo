using FluentValidation;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Resources;

namespace IPTS.Validators.Admin
{
    public class UserFormValidator : AbstractValidator<UserFormViewModel>
    {
        public UserFormValidator(LocService localizer)
        {
            When(x => string.IsNullOrEmpty(x.Id), () =>
            {
                RuleFor(x => x.UserName).NotEmpty().WithMessage(localizer.GetSystem("UsernameRequired"));
                RuleFor(x => x.UserName).Length(3, 20).WithMessage(localizer.GetSystem("UsernameMaxLength"));

                RuleFor(x => x.Password).NotEmpty().WithMessage(localizer.GetSystem("PasswordRequired"));
                RuleFor(x => x.Password).MinimumLength(6).WithMessage(localizer.GetSystem("PasswordMinLength"));
                RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage(localizer.GetSystem("PasswordsDoNotMatch"));
            });

            RuleFor(x => x.Email).NotEmpty().WithMessage(localizer.GetSystem("EmailRequired")).EmailAddress().WithMessage(localizer.GetSystem("InvalidEmailFormat"));

            When(x => x.Doctor != null, () => RuleFor(x => x.Doctor).SetValidator(new DoctorFormValidator(localizer)));
            When(x => x.Patient != null, () => RuleFor(x => x.Patient).SetValidator(new PatientFormValidator(localizer)));
            When(x => x.Customer != null, () => RuleFor(x => x.Customer).SetValidator(new CustomerFormValidator(localizer)));
        }
    }
}
