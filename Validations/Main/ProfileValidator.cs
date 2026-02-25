using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class ProfileValidator : AbstractValidator<ProfileViewModel>
    {
        public ProfileValidator(LocService localizer)
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage(x => localizer.GetSystem("UserIdRequired"));
            RuleFor(x => x.Email).NotEmpty().WithMessage(x => localizer.GetSystem("EmailRequired")).EmailAddress().WithMessage(x => localizer.GetSystem("InvalidEmailFormat"));
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage(x => localizer.GetSystem("PhoneRequired"));
            RuleFor(x => x.UserTypeName).NotEmpty().WithMessage(x => localizer.GetSystem("UserTypeRequired"));
        }
    }
}
