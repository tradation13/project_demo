using FluentValidation;
using IPTS.Areas.Doctor.ViewsModels;
using IPTS.Resources;
using IPTS.Validators;

namespace IPTS.Validators.Doctor
{
    public class PatientRegistrationValidator : AbstractValidator<PatientRegistrationViewModel>
    {
        public PatientRegistrationValidator(LocService localizer)
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage(localizer.GetSystem("UsernameRequired"))
                .Length(3, 20).WithMessage(localizer.GetSystem("UsernameMaxLength"))
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage(localizer.GetSystem("InvalidUsernameFormat"));

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage(localizer.GetSystem("FirstNameRequired"));

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage(localizer.GetSystem("LastNameRequired"));

            // RuleFor(x => x.NationalId)
            //     .NotEmpty().WithMessage(localizer.GetSystem("IdentityRequired"))
            //     .Length(10).WithMessage(localizer.GetSystem("InvalidId"))
            //     .Matches(@"^\d{10}$").WithMessage(localizer.GetSystem("IdentityMustBeNumbers"));

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage(localizer.GetSystem("PhoneRequired"))
               .Must(x => x != null && !x.Any(char.IsLetter))
.WithMessage(localizer.GetSystem("InvalidPhoneFormat"));

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(localizer.GetSystem("EmailRequired"))
                .EmailAddress().WithMessage(localizer.GetSystem("InvalidEmailFormat"));

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage(localizer.GetSystem("BirthDateRequired"))
                .LessThan(DateTime.Now.Date).WithMessage(localizer.GetSystem("InvalidBirthDate"));

            this.AddPatientHealthRules(localizer);
        }
    }
}
