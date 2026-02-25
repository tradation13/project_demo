using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class AdminProfileValidator : AbstractValidator<AdminProfileViewModel>
    {
        public AdminProfileValidator(LocService localizer)
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidId"));
        }
    }
}
