using IPTS.Models.Enums;

namespace IPTS.ViewModels
{
    public interface IPatientHealthFields
    {
        float? Weight { get; set; }
        float? Height { get; set; }
        EnBloodGroup? BloodGroup { get; set; }
        bool? IsSmoker { get; set; }
        bool? HasChronicDisease { get; set; }
    }
}
