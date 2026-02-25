using FluentValidation;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Resources;

namespace IPTS.Validators.Admin
{
    public class DoctorFormValidator : AbstractValidator<DoctorFormViewModel>
    {
        public DoctorFormValidator(LocService localizer)
        {
            RuleFor(x => x.SpecialtyId).GreaterThan(0).WithMessage(localizer.GetSystem("SpecialtyRequired"));
        }
    }
}
