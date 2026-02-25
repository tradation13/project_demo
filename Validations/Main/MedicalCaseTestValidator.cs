using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class MedicalCaseTestValidator : AbstractValidator<MedicalCaseTestViewModel>
    {
        public MedicalCaseTestValidator(LocService localizer)
        {
            RuleFor(x => x.TestId).GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidTestId"));
            RuleFor(x => x.MedicalCaseId).GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidMedicalCaseId"));
            RuleFor(x => x.TestName).NotEmpty().WithMessage(x => localizer.GetSystem("TestNameRequired"));
        }
    }
}
