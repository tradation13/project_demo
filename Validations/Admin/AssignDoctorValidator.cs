using FluentValidation;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Resources;

namespace IPTS.Validators.Admin
{
    public class AssignDoctorValidator : AbstractValidator<AssignDoctorViewModel>
    {
        public AssignDoctorValidator(LocService localizer)
        {
            RuleFor(x => x.PatientId)
                .GreaterThan(0)
                .WithMessage(localizer.GetSystem("AssignDoctor_PatientRequired"));

            RuleFor(x => x.AssignedDoctorId)
                .GreaterThan(0)
                .WithMessage(localizer.GetSystem("AssignDoctor_DoctorRequired"));
        }
    }
}
