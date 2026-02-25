using FluentValidation;
using IPTS.Areas.Doctor.ViewsModels;
using IPTS.Resources;

namespace IPTS.Validators.Doctor
{
    public class AcceptAppointmentValidator : AbstractValidator<AcceptAppointmentViewModel>
    {
        public AcceptAppointmentValidator(LocService localizer)
        {
            RuleFor(x => x.SelectedSlots).NotNull().WithMessage(localizer.GetSystem("Val_RequiredTimeSlot"));
            RuleFor(x => x.SelectedSlots).Must(s => s != null && s.Any()).WithMessage(localizer.GetSystem("Val_RequiredTimeSlot"));
        }
    }
}
