using System.ComponentModel.DataAnnotations;
using IPTS.Models.Enums;

namespace IPTS.ViewModels
{
    public class PatientProfileViewModel : IPatientHealthFields
    {
        public int? Id { get; set; }
        
        // [Display(Name = "Identity Number")]
        // public string IdentityNumber { get; set; } = string.Empty;
        
        [Display(Name = "Birth Date")]
        public DateTime BirthDate { get; set; }
        
        public string? UserId { get; set; }

        public float? Weight { get; set; }
        public float? Height { get; set; }
        public EnBloodGroup? BloodGroup { get; set; }
        public bool? IsSmoker { get; set; }
        public bool? HasChronicDisease { get; set; }
    }
}
