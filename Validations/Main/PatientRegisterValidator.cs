using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class PatientRegisterValidator : AbstractValidator<PatientRegisterViewModel>
    {
        public PatientRegisterValidator(LocService localizer)
        {
            // RuleFor(x => x.IdentityNumber).NotEmpty().WithMessage(x => localizer.GetSystem("IdentityRequired")).MaximumLength(50).WithMessage(x => localizer.GetSystem("IdentityMaxLength"));
            RuleFor(x => x.BirthDate).NotEmpty().WithMessage(x => localizer.GetSystem("BirthDateRequired"));
            this.AddPatientHealthRules(localizer);
        }
    }
}
