using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class LogValidator : AbstractValidator<LogViewModel>
    {
        public LogValidator(LocService localizer)
        {
            RuleFor(x => x.Level).NotEmpty().WithMessage(x => localizer.GetSystem("LevelRequired"));
            RuleFor(x => x.Description).NotEmpty().WithMessage(x => localizer.GetSystem("DescriptionRequired"));
            RuleFor(x => x.Timestamp).NotEmpty().WithMessage(x => localizer.GetSystem("TimestampRequired"));
        }
    }
}
