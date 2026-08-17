using FluentValidation;
using IPTS.Resources;
using IPTS.ViewModels;

namespace IPTS.Validators
{
    public static class PatientHealthFieldsRules
    {
        public static void AddPatientHealthRules<T>(this AbstractValidator<T> validator, LocService localizer)
            where T : IPatientHealthFields
        {
            validator.RuleFor(x => x.Weight)
                .InclusiveBetween(1, 500)
                .When(x => x.Weight.HasValue)
                .WithMessage(_ => localizer.GetSystem("InvalidWeight"));

            validator.RuleFor(x => x.Height)
                .InclusiveBetween(30, 300)
                .When(x => x.Height.HasValue)
                .WithMessage(_ => localizer.GetSystem("InvalidHeight"));
        }
    }
}
