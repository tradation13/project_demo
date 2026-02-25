using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class BookAppointmentDtoValidator : AbstractValidator<BookAppointmentDto>
    {
        public BookAppointmentDtoValidator(LocService localizer)
        {
            RuleFor(x => x.PatientId).GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidPatientId"));
            RuleFor(x => x.DoctorId).GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidDoctorId"));
            RuleFor(x => x.TimeSlot).NotEmpty().WithMessage(x => localizer.GetSystem("TimeSlotRequired"));
        }
    }
}
