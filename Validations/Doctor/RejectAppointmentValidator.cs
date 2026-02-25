using FluentValidation;
using IPTS.Areas.Doctor.ViewsModels;
using IPTS.Resources;

namespace IPTS.Validators.Doctor
{
    public class RejectAppointmentValidator : AbstractValidator<RejectAppointmentViewModel>
    {
        public RejectAppointmentValidator(LocService localizer)
        {
            RuleFor(x => x.RejectReason).NotEmpty().WithMessage(localizer.GetSystem("Val_RejectionReasonRequired"));
        }
    }
}
