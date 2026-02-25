using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class AppointmentTimeSlotValidator : AbstractValidator<AppointmentTimeSlotViewModel>
    {
        public AppointmentTimeSlotValidator(LocService localizer)
        {
            RuleFor(x => x.Time).NotEmpty().WithMessage(x => localizer.GetSystem("TimeRequired"));
            RuleFor(x => x.SlotIndex).GreaterThanOrEqualTo(0).WithMessage(x => localizer.GetSystem("InvalidSlotIndex"));
        }
    }
}
