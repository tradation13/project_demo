using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class PatientSearchValidator : AbstractValidator<PatientSearchViewModel>
    {
        public PatientSearchValidator(LocService localizer)
        {
            RuleFor(x => x.SearchTerm).NotEmpty().WithMessage(x => localizer.GetSystem("SearchTermRequired"));
            RuleFor(x => x.SearchType).NotEmpty().WithMessage(x => localizer.GetSystem("SearchTypeRequired"));
        }
    }
}
