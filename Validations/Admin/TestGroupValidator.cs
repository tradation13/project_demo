using FluentValidation;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Resources;

namespace IPTS.Validators.Admin
{
    public class TestGroupValidator : AbstractValidator<TestGroupViewModel>
    {
        public TestGroupValidator(LocService localizer)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(localizer.GetSystem("NameRequired"));
        }
    }
}
