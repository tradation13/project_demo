using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class PatientRegisterValidator : AbstractValidator<PatientRegisterViewModel>
    {
        public PatientRegisterValidator(LocService localizer)
        {
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
