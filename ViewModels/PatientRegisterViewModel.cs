using System.ComponentModel.DataAnnotations;
using IPTS.Models.Enums;

namespace IPTS.ViewModels
{
    public class PatientRegisterViewModel : IPatientHealthFields
    {
        public int? Id { get; set; }
        
        // // [Required(ErrorMessage = "Identity Number is required")]
        // [StringLength(50, ErrorMessage = "Identity Number cannot exceed 50 characters")]
        // [Display(Name = "Identity Number")]
        // public string IdentityNumber { get; set; } = string.Empty;
        
        [Display(Name = "Birth Date")]
        public DateTime? BirthDate { get; set; }
        
        public string? UserId { get; set; }

        public float? Weight { get; set; }
        public float? Height { get; set; }
        public EnBloodGroup? BloodGroup { get; set; }
        public bool? IsSmoker { get; set; }
        public bool? HasChronicDisease { get; set; }
    }
}
