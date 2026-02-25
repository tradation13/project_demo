using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class CustomerRegisterValidator : AbstractValidator<CustomerRegisterViewModel>
    {
        public CustomerRegisterValidator(LocService localizer)
        {
            RuleFor(x => x.FirstName).NotEmpty().WithMessage(x => localizer.GetSystem("FirstNameRequired")).MaximumLength(50).WithMessage(x => localizer.GetSystem("FirstNameMaxLength"));
            RuleFor(x => x.LastName).NotEmpty().WithMessage(x => localizer.GetSystem("LastNameRequired")).MaximumLength(50).WithMessage(x => localizer.GetSystem("LastNameMaxLength"));
        }
    }
}
