using FluentValidation;
using IPTS.Areas.Patient.ViewsModels;
using IPTS.Resources;

namespace IPTS.Validators.Patient
{
    public class PatientPanelValidator : AbstractValidator<PatientPanelViewModel>
    {
        public PatientPanelValidator(LocService localizer)
        {
            RuleFor(x => x.AppointmentsCount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MedicalCasesCount).GreaterThanOrEqualTo(0);
        }
    }
}
