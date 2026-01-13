namespace IPTS.Areas.Doctor.ViewsModels
{
    public record StatCardViewModel(
           string Title,
           string Value,
           string? Subtitle,
           string IconCss,
           string CardCss
       );

}
