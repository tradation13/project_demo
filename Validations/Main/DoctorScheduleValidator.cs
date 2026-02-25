using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class DoctorScheduleValidator : AbstractValidator<DoctorScheduleViewModel>
    {
        public DoctorScheduleValidator(LocService localizer)
        {
            RuleFor(x => x.Doctor).NotNull().WithMessage(x => localizer.GetSystem("DoctorRequired"));
            RuleFor(x => x.TimeSlots).NotNull().WithMessage(x => localizer.GetSystem("TimeSlotsRequired"));
            RuleFor(x => x.SelectedDate).NotEmpty().WithMessage(x => localizer.GetSystem("SelectedDateRequired"));
            When(x => x.Doctor != null, () => RuleFor(x => x.Doctor).SetValidator(new DoctorValidator(localizer)));
            RuleForEach(x => x.TimeSlots).SetValidator(new AppointmentTimeSlotValidator(localizer));
        }
    }
}
