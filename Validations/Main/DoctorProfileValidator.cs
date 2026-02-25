using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class DoctorProfileValidator : AbstractValidator<DoctorProfileViewModel>
    {
        public DoctorProfileValidator(LocService localizer)
        {
            RuleFor(x => x.SpecialtyId).GreaterThan(0).WithMessage(x => localizer.GetSystem("SpecialtyRequired"));
        }
    }
}
