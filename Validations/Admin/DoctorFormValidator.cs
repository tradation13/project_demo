using FluentValidation;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Resources;

namespace IPTS.Validators.Admin
{
    public class DoctorFormValidator : AbstractValidator<DoctorFormViewModel>
    {
        public DoctorFormValidator(LocService localizer)
        {
            RuleFor(x => x.SpecialtyId).GreaterThan(0).WithMessage(localizer.GetSystem("SpecialtyRequired"));

            RuleFor(x => x.PhotoFile)
                .Must(file => file == null || (file.ContentType.StartsWith("image/") &&
                    (file.ContentType == "image/jpeg" || file.ContentType == "image/png" || file.ContentType == "image/webp" || file.ContentType == "image/gif")))
                .WithMessage(localizer.GetSystem("DoctorPhotoImageOnly"));
        }
    }
}
