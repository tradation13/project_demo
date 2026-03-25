using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class AppointmentValidator : AbstractValidator<AppointmentViewModel>
    {
        public AppointmentValidator(LocService localizer)
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidId"));
            RuleFor(x => x.PatientName).NotEmpty().WithMessage(x => localizer.GetSystem("PatientNameRequired")).MaximumLength(150).WithMessage(x => localizer.GetSystem("PatientNameMaxLength"));
            // RuleFor(x => x.PatientIdentityNumber).NotEmpty().WithMessage(x => localizer.GetSystem("IdentityRequired")).MaximumLength(50).WithMessage(x => localizer.GetSystem("IdentityMaxLength"));
            RuleFor(x => x.PatientPhone).NotEmpty().WithMessage(x => localizer.GetSystem("PhoneRequired")).Matches(@"^\+?[1-9]\d{1,14}$").WithMessage(x => localizer.GetSystem("InvalidPhoneFormat"));
            RuleFor(x => x.PatientEmail).NotEmpty().WithMessage(x => localizer.GetSystem("EmailRequired")).EmailAddress().WithMessage(x => localizer.GetSystem("InvalidEmailFormat"));
            RuleFor(x => x.DoctorId).GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidDoctorId"));
            RuleFor(x => x.ScheduledTime).NotEmpty().WithMessage(x => localizer.GetSystem("ScheduledTimeRequired"));
            RuleFor(x => x.Notes).MaximumLength(2000).WithMessage(x => localizer.GetSystem("NotesMaxLength"));
        }
    }
}
