using FluentValidation;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Resources;
using IPTS.Validators;

namespace IPTS.Validators.Admin
{
    public class PatientFormValidator : AbstractValidator<PatientFormViewModel>
    {
        public PatientFormValidator(LocService localizer)
        {
            // RuleFor(x => x.IdentityNumber).NotEmpty().WithMessage(localizer.GetSystem("IdentityRequired"));
            RuleFor(x => x.IdentityNumber).MaximumLength(50).WithMessage(localizer.GetSystem("IdentityMaxLength"));
            RuleFor(x => x.BirthDate).NotEmpty().WithMessage(localizer.GetSystem("BirthDateRequired"));
            RuleFor(x => x.BirthDate).LessThan(DateTime.UtcNow).WithMessage(localizer.GetSystem("InvalidBirthDate"));
            this.AddPatientHealthRules(localizer);
        }
    }
}
