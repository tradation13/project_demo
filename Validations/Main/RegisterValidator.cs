using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class RegisterValidator : AbstractValidator<RegisterViewModel>
    {
        public RegisterValidator(LocService localizer)
        {
            When(x => !string.IsNullOrWhiteSpace(x.UserName), () =>
            {
                RuleFor(x => x.UserName)
                    .MaximumLength(50).WithMessage(x => localizer.GetSystem("UsernameMaxLength"));
            });

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(x => localizer.GetSystem("EmailRequired"))
                .EmailAddress().WithMessage(x => localizer.GetSystem("InvalidEmailFormat"));

            When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
            {
                RuleFor(x => x.PhoneNumber)
                    .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage(x => localizer.GetSystem("InvalidPhoneFormat"));
            });

            // FirstName
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage(x => localizer.GetSystem("FirstNameRequired"))
                .MaximumLength(50).WithMessage(x => localizer.GetSystem("FirstNameMaxLength"));

            // LastName
            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage(x => localizer.GetSystem("LastNameRequired"))
                .MaximumLength(50).WithMessage(x => localizer.GetSystem("LastNameMaxLength"));

            // Password
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(x => localizer.GetSystem("PasswordRequired"))
                .MinimumLength(6).WithMessage(x => localizer.GetSystem("PasswordMinLength"));

            // ConfirmPassword
            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage(x => localizer.GetSystem("ConfirmPasswordRequired"))
                .Equal(x => x.Password).WithMessage(x => localizer.GetSystem("PasswordsDoNotMatch"));

            // UserTypeName
            RuleFor(x => x.UserTypeName)
                .NotEmpty().WithMessage(x => localizer.GetSystem("UserTypeRequired"));

            RuleFor(x => x.AcceptPrivacy)
                .Equal(true).WithMessage(x => localizer.GetSystem("PrivacyPolicyRequired"));

            RuleFor(x => x.AcceptTerms)
                .Equal(true).WithMessage(x => localizer.GetSystem("TermsOfUseRequired"));

            // Patient registration requires explicit health-data consent (unchecked by default).
            When(x => string.Equals(x.UserTypeName, "patient", StringComparison.OrdinalIgnoreCase)
                      || x.Patient != null, () =>
            {
                RuleFor(x => x.AcceptHealthDataConsent)
                    .Equal(true).WithMessage(x => localizer.GetSystem("HealthDataConsentRequired"));
            });

            // Nested validators for specific user types
            When(x => x.Customer != null, () =>
            {
                RuleFor(x => x.Customer).SetValidator(new CustomerRegisterValidator(localizer));
            });

            When(x => x.Patient != null, () =>
            {
                RuleFor(x => x.Patient).SetValidator(new PatientRegisterValidator(localizer));
            });

            When(x => x.Doctor != null, () =>
            {
                RuleFor(x => x.Doctor).SetValidator(new DoctorRegisterValidator(localizer));
            });
        }
    }
}