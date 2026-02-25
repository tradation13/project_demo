using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class AppointmentScheduleValidator : AbstractValidator<AppointmentScheduleViewModel>
    {
        public AppointmentScheduleValidator(LocService localizer)
        {
            RuleFor(x => x.SelectedDate).NotEmpty().WithMessage(x => localizer.GetSystem("SelectedDateRequired"));
            RuleFor(x => x.TimeSlots).NotNull().WithMessage(x => localizer.GetSystem("TimeSlotsRequired"));
            RuleForEach(x => x.TimeSlots).SetValidator(new AppointmentTimeSlotValidator(localizer));
        }
    }
}
