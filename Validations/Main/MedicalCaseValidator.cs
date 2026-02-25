using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class MedicalCaseValidator : AbstractValidator<MedicalCaseViewModel>
    {
        public MedicalCaseValidator(LocService localizer)
        {
            RuleFor(x => x.Id).GreaterThanOrEqualTo(0).WithMessage(x => localizer.GetSystem("InvalidId"));
            RuleFor(x => x.Name).NotEmpty().WithMessage(x => localizer.GetSystem("NameRequired")).MaximumLength(200).WithMessage(x => localizer.GetSystem("NameMaxLength"));
            RuleFor(x => x.Description).NotEmpty().WithMessage(x => localizer.GetSystem("DescriptionRequired")).MaximumLength(2000).WithMessage(x => localizer.GetSystem("DescriptionMaxLength"));
            RuleFor(x => x.CreatedAt).NotEmpty().WithMessage(x => localizer.GetSystem("CreatedAtRequired"));
            RuleFor(x => x.DoctorId).GreaterThanOrEqualTo(0).WithMessage(x => localizer.GetSystem("InvalidDoctorId"));
            RuleFor(x => x.PatientId).GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidPatientId"));
            RuleForEach(x => x.Tests).SetValidator(new MedicalCaseTestValidator(localizer));
        }
    }
}
