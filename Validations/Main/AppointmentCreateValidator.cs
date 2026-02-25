using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class AppointmentCreateValidator : AbstractValidator<AppointmentCreateViewModel>
    {
        public AppointmentCreateValidator(LocService localizer)
        {
            RuleFor(x => x.PatientId).GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidPatientId"));
            RuleFor(x => x.DoctorId).GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidDoctorId"));
            RuleFor(x => x.ScheduledDate).NotEmpty().WithMessage(x => localizer.GetSystem("ScheduledDateRequired"));
            RuleFor(x => x.ScheduledTime).NotEmpty().WithMessage(x => localizer.GetSystem("ScheduledTimeRequired"));
            RuleFor(x => x.StartSlotIndex).GreaterThanOrEqualTo(0).WithMessage(x => localizer.GetSystem("InvalidSlotIndex"));
            RuleFor(x => x.EndSlotIndex).GreaterThanOrEqualTo(x => x.StartSlotIndex).WithMessage(x => localizer.GetSystem("InvalidSlotRange"));
        }
    }
}
