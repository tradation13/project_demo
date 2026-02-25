using FluentValidation;
using IPTS.ViewModels;
using IPTS.Resources;

namespace IPTS.Validators
{
    public class DoctorValidator : AbstractValidator<DoctorViewModel>
    {
        public DoctorValidator(LocService localizer)
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage(x => localizer.GetSystem("InvalidId"));
            RuleFor(x => x.UserId).NotEmpty().WithMessage(x => localizer.GetSystem("UserIdRequired"));
            RuleFor(x => x.FullName).NotEmpty().WithMessage(x => localizer.GetSystem("FullNameRequired")).MaximumLength(200).WithMessage(x => localizer.GetSystem("FullNameMaxLength"));
            RuleFor(x => x.Specialty).NotEmpty().WithMessage(x => localizer.GetSystem("SpecialtyRequired"));
            When(x => x.Rating.HasValue, () => {
                RuleFor(x => x.Rating).InclusiveBetween(0,5).WithMessage(x => localizer.GetSystem("InvalidRating"));
            });
        }
    }
}
