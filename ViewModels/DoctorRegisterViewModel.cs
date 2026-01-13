using System.ComponentModel.DataAnnotations;

namespace IPTS.ViewModels
{
    public class DoctorRegisterViewModel
    {
        public int? Id { get; set; }
        
        [Required(ErrorMessage = "Medical Specialty is required")]
        [Display(Name = "Medical Specialty")]
        public int SpecialtyId { get; set; }
        
        public string? UserId { get; set; }
    }
}
