using FluentValidation;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Resources;

namespace IPTS.Validators.Admin
{
    public class CustomerFormValidator : AbstractValidator<CustomerFormViewModel>
    {
        public CustomerFormValidator(LocService localizer)
        {
            RuleFor(x => x.FirstName).NotEmpty().WithMessage(localizer.GetSystem("FirstNameRequired"));
            RuleFor(x => x.LastName).NotEmpty().WithMessage(localizer.GetSystem("LastNameRequired"));
        }
    }
}
