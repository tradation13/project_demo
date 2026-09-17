using FluentValidation;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Resources;

namespace IPTS.Validators.Admin
{
    public class TestValidator : AbstractValidator<TestViewModel>
    {
        public TestValidator(LocService localizer)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(localizer.GetSystem("NameRequired"));
            RuleFor(x => x.TestGroupId).GreaterThan(0).WithMessage(localizer.GetSystem("TestGroupRequired"));
            RuleFor(x => x.StandardValue)
                .Must(v => !v.HasValue || (v.Value >= -100000m && v.Value <= 1000000m))
                .WithMessage(localizer.GetSystem("InvalidStandardValue"));
        }
    }
}
