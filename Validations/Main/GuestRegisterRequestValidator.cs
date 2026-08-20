using FluentValidation;
using IPTS.Resources;
using IPTS.ViewModels;

namespace IPTS.Validators
{
    public class GuestRegisterRequestValidator : AbstractValidator<GuestRegisterRequest>
    {
        public GuestRegisterRequestValidator(LocService localizer)
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage(_ => localizer.GetSystem("FirstNameRequired"))
                .MaximumLength(50).WithMessage(_ => localizer.GetSystem("FirstNameMaxLength"));

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage(_ => localizer.GetSystem("LastNameRequired"))
                .MaximumLength(50).WithMessage(_ => localizer.GetSystem("LastNameMaxLength"));

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(_ => localizer.GetSystem("EmailRequired"))
                .EmailAddress().WithMessage(_ => localizer.GetSystem("InvalidEmailFormat"));

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(_ => localizer.GetSystem("PasswordRequired"))
                .MinimumLength(6).WithMessage(_ => localizer.GetSystem("PasswordMinLength"));

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage(_ => localizer.GetSystem("ConfirmPasswordRequired"))
                .Equal(x => x.Password).WithMessage(_ => localizer.GetSystem("PasswordsDoNotMatch"));

            RuleFor(x => x.AcceptPrivacy)
                .Equal(true).WithMessage(_ => localizer.GetSystem("PrivacyPolicyRequired"));

            RuleFor(x => x.AcceptTerms)
                .Equal(true).WithMessage(_ => localizer.GetSystem("TermsOfUseRequired"));

            RuleFor(x => x.AcceptHealthDataConsent)
                .Equal(true).WithMessage(_ => localizer.GetSystem("HealthDataConsentRequired"));
        }
    }
}
