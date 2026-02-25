using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class RegisterValidator : AbstractValidator<RegisterViewModel>
    {
        public RegisterValidator(LocService localizer)
        {
            // UserName
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage(x => localizer.GetSystem("UsernameRequired"))
                .MaximumLength(50).WithMessage(x => localizer.GetSystem("UsernameMaxLength"));

            // Email
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(x => localizer.GetSystem("EmailRequired"))
                .EmailAddress().WithMessage(x => localizer.GetSystem("InvalidEmailFormat"));

            // PhoneNumber
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage(x => localizer.GetSystem("PhoneRequired"))
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage(x => localizer.GetSystem("InvalidPhoneFormat"));

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