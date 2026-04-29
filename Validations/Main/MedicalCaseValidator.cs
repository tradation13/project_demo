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
            // --- قواعد الـ Physical Vitals الجديدة ---

    // التحقق من الوزن (إذا تم إدخاله يجب أن يكون أكبر من 0 وأقل من 500 مثلاً)
    RuleFor(x => x.Weight)
        .InclusiveBetween(1, 500)
        .When(x => x.Weight.HasValue)
        .WithMessage(x => localizer.GetSystem("InvalidWeight"));

    // التحقق من الطول (إذا تم إدخاله يجب أن يكون بين 30 و 300 سم)
    RuleFor(x => x.Height)
        .InclusiveBetween(30, 300)
        .When(x => x.Height.HasValue)
        .WithMessage(x => localizer.GetSystem("InvalidHeight"));

            // --- الحقول الإضافية: تقبل null أو أي نص ---
            RuleFor(x => x.InjuryHistory)
                .MaximumLength(2000)
                .When(x => x.InjuryHistory != null);

            RuleFor(x => x.Medications)
                .MaximumLength(2000)
                .When(x => x.Medications != null);

            RuleFor(x => x.FunctionalAbility)
                .MaximumLength(2000)
                .When(x => x.FunctionalAbility != null);

            RuleFor(x => x.PersonalGoals)
                .MaximumLength(2000)
                .When(x => x.PersonalGoals != null);
        }
    }
}
