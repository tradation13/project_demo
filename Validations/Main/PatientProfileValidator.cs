using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class PatientProfileValidator : AbstractValidator<PatientProfileViewModel>
    {
        public PatientProfileValidator(LocService localizer)
        {
            When(x => x.Id.HasValue, () => RuleFor(x => x.Id.Value).GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidId")));
            // RuleFor(x => x.IdentityNumber).MaximumLength(50).WithMessage(x => localizer.GetSystem("IdentityMaxLength"));
            RuleFor(x => x.BirthDate).NotEmpty().WithMessage(x => localizer.GetSystem("BirthDateRequired"));
            this.AddPatientHealthRules(localizer);
        }
    }
}
