using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class SingleAppointmentCreateValidator : AbstractValidator<SingleAppointmentCreateViewModel>
    {
        public SingleAppointmentCreateValidator(LocService localizer)
        {
            // PatientId and DoctorId are populated on the server (from the authenticated user and route).
            // Validating them here causes client-submitted models (which don't include those values) to fail.
            // Server-side checks for patient/doctor existence remain in the controller.
            RuleFor(x => x.ScheduledDate).NotEmpty().WithMessage(x => localizer.GetSystem("ScheduledDateRequired"));
            RuleFor(x => x.SlotIndex).GreaterThanOrEqualTo(0).WithMessage(x => localizer.GetSystem("InvalidSlotIndex"));
            RuleFor(x => x.Time).NotEmpty().WithMessage(x => localizer.GetSystem("StartTimeRequired"));
        }
    }
}
