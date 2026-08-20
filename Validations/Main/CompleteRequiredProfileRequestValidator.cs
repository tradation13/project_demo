using FluentValidation;
using IPTS.Resources;
using IPTS.ViewModels;

namespace IPTS.Validators
{
    public class CompleteRequiredProfileRequestValidator : AbstractValidator<CompleteRequiredProfileRequest>
    {
        public CompleteRequiredProfileRequestValidator(LocService localizer)
        {
            RuleFor(x => x.BirthDate)
                .NotEmpty().WithMessage(_ => localizer.GetSystem("BirthDateRequired"))
                .LessThanOrEqualTo(DateTime.Today).WithMessage(_ => localizer.GetSystem("InvalidBirthDate"));

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage(_ => localizer.GetSystem("PhoneRequired"))
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage(_ => localizer.GetSystem("InvalidPhoneFormat"));
        }
    }
}
