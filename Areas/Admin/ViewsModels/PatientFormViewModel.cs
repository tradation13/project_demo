using System.ComponentModel.DataAnnotations;
using IPTS.Models.Enums;
using IPTS.ViewModels;

namespace IPTS.Areas.Admin.ViewsModels
{
    public class PatientFormViewModel : IPatientHealthFields
    {
        public int? Id { get; set; }
        
        public string IdentityNumber { get; set; } = string.Empty;
        
        [Display(Name = "Label_BirthDate")]
        public DateTime BirthDate { get; set; } = DateTime.Today;
        
        public string? UserId { get; set; }

        public float? Weight { get; set; }
        public float? Height { get; set; }
        public EnBloodGroup? BloodGroup { get; set; }
        public bool? IsSmoker { get; set; }
        public bool? HasChronicDisease { get; set; }
    }
}
