using FluentValidation;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Resources;

namespace IPTS.Validators.Admin
{
    public class TestParameterValidator : AbstractValidator<TestParameterViewModel>
    {
        public TestParameterValidator(LocService localizer)
        {
            RuleFor(x => x.TestId).GreaterThan(0).WithMessage(localizer.GetSystem("InvalidTestId"));
            RuleFor(x => x.Key).NotEmpty().WithMessage(localizer.GetSystem("ParameterKeyRequired"));
        }
    }
}
