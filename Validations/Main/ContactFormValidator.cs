using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class ContactFormValidator : AbstractValidator<ContactFormViewModel>
    {
        public ContactFormValidator(LocService localizer)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(x => localizer.GetSystem("NameRequired")).MaximumLength(100).WithMessage(x => localizer.GetSystem("NameMaxLength"));
            RuleFor(x => x.Email).NotEmpty().WithMessage(x => localizer.GetSystem("EmailRequired")).EmailAddress().WithMessage(x => localizer.GetSystem("InvalidEmailFormat"));
            RuleFor(x => x.Phone).MaximumLength(30).WithMessage(x => localizer.GetSystem("PhoneMaxLength"));
            RuleFor(x => x.Message).NotEmpty().WithMessage(x => localizer.GetSystem("MessageRequired")).MaximumLength(2000).WithMessage(x => localizer.GetSystem("MessageMaxLength"));
        }
    }
}
