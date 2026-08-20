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
            When(x => x.BirthDate.HasValue, () =>
            {
                RuleFor(x => x.BirthDate!.Value)
                    .LessThanOrEqualTo(DateTime.Today)
                    .WithMessage(x => localizer.GetSystem("InvalidBirthDate"));
            });
            this.AddPatientHealthRules(localizer);
        }
    }
}
