using FluentValidation;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Resources;

namespace IPTS.Validators.Admin
{
    public class BlogPostValidator : AbstractValidator<BlogPostViewModel>
    {
        public BlogPostValidator(LocService localizer)
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage(localizer.GetSystem("Blog_TitleRequired"));

            RuleFor(x => x.ShortDescription)
                .NotEmpty().WithMessage(localizer.GetSystem("Blog_ShortRequired"));

            RuleFor(x => x.LongDescription)
                .NotEmpty().WithMessage(localizer.GetSystem("Blog_LongRequired"));

            When(x => x.IsNewPost, () =>
            {
                RuleFor(x => x.Files)
                    .Must(files => files != null && files.Any(f => f != null && f.Length > 0))
                    .WithMessage(localizer.GetSystem("Blog_ImageRequired"));
            });
        }
    }
}
