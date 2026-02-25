using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class UserListValidator : AbstractValidator<UserListViewModel>
    {
        public UserListValidator(LocService localizer)
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage(x => localizer.GetSystem("UserIdRequired"));
            RuleFor(x => x.UserTypeName).NotEmpty().WithMessage(x => localizer.GetSystem("UserTypeRequired"));
            RuleFor(x => x.UserName).NotEmpty().WithMessage(x => localizer.GetSystem("UserNameRequired"));
            RuleFor(x => x.Email).NotEmpty().WithMessage(x => localizer.GetSystem("EmailRequired")).EmailAddress().WithMessage(x => localizer.GetSystem("InvalidEmailFormat"));
        }
    }
}
