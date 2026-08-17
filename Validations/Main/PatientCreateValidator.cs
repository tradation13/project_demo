using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class PatientCreateValidator : AbstractValidator<PatientCreateViewModel>
    {
        public PatientCreateValidator(LocService localizer)
        {
            RuleFor(x => x.UserName).NotEmpty().WithMessage(x => localizer.GetSystem("UsernameRequired")).MaximumLength(50).WithMessage(x => localizer.GetSystem("UsernameMaxLength"));
            RuleFor(x => x.Email).NotEmpty().WithMessage(x => localizer.GetSystem("EmailRequired")).EmailAddress().WithMessage(x => localizer.GetSystem("InvalidEmailFormat"));
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage(x => localizer.GetSystem("PhoneRequired")).Matches(@"^\+?[1-9]\d{1,14}$").WithMessage(x => localizer.GetSystem("InvalidPhoneFormat"));
            RuleFor(x => x.FirstName).NotEmpty().WithMessage(x => localizer.GetSystem("FirstNameRequired")).MaximumLength(50).WithMessage(x => localizer.GetSystem("FirstNameMaxLength"));
            RuleFor(x => x.LastName).NotEmpty().WithMessage(x => localizer.GetSystem("LastNameRequired")).MaximumLength(50).WithMessage(x => localizer.GetSystem("LastNameMaxLength"));
            RuleFor(x => x.Password).NotEmpty().WithMessage(x => localizer.GetSystem("PasswordRequired")).MinimumLength(6).WithMessage(x => localizer.GetSystem("PasswordMinLength"));
            RuleFor(x => x.ConfirmPassword).NotEmpty().WithMessage(x => localizer.GetSystem("ConfirmPasswordRequired")).Equal(x => x.Password).WithMessage(x => localizer.GetSystem("PasswordsDoNotMatch"));
            // RuleFor(x => x.IdentityNumber).NotEmpty().WithMessage(x => localizer.GetSystem("IdentityRequired")).MaximumLength(50).WithMessage(x => localizer.GetSystem("IdentityMaxLength"));
            RuleFor(x => x.BirthDate).NotEmpty().WithMessage(x => localizer.GetSystem("BirthDateRequired")).LessThan(DateTime.Now).WithMessage(x => localizer.GetSystem("BirthDateInvalid"));
            this.AddPatientHealthRules(localizer);
        }
    }
}
