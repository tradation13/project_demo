using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class AppointmentEditValidator : AbstractValidator<AppointmentEditViewModel>
    {
        public AppointmentEditValidator(LocService localizer)
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidId"));
            RuleFor(x => x.PatientId).GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidPatientId"));
            RuleFor(x => x.DoctorId).GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidDoctorId"));
            RuleFor(x => x.ScheduledTime).NotEmpty().WithMessage(x => localizer.GetSystem("ScheduledTimeRequired"));
        }
    }
}
