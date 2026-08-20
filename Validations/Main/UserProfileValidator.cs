using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class UserProfileValidator : AbstractValidator<UserProfileViewModel>
    {
        public UserProfileValidator(LocService localizer)
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage(x => localizer.GetSystem("UserIdRequired"));
            RuleFor(x => x.Email).NotEmpty().WithMessage(x => localizer.GetSystem("EmailRequired")).EmailAddress().WithMessage(x => localizer.GetSystem("InvalidEmailFormat"));
            RuleFor(x => x.FirstName).NotEmpty().WithMessage(x => localizer.GetSystem("FirstNameRequired"));
            RuleFor(x => x.LastName).NotEmpty().WithMessage(x => localizer.GetSystem("LastNameRequired"));
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage(x => localizer.GetSystem("UsernameRequired"))
                .MaximumLength(50).WithMessage(x => localizer.GetSystem("UsernameMaxLength"));
            When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
            {
                RuleFor(x => x.PhoneNumber)
                    .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage(x => localizer.GetSystem("InvalidPhoneFormat"));
            });
            When(x => x.Admin != null, () => RuleFor(x => x.Admin).SetValidator(new AdminProfileValidator(localizer)));
            When(x => x.Customer != null, () => RuleFor(x => x.Customer).SetValidator(new CustomerProfileValidator(localizer)));
            When(x => x.Patient != null, () => RuleFor(x => x.Patient).SetValidator(new PatientProfileValidator(localizer)));
            When(x => x.Doctor != null, () => RuleFor(x => x.Doctor).SetValidator(new DoctorProfileValidator(localizer)));
        }
    }
}
