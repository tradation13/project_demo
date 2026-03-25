using System.ComponentModel.DataAnnotations;

namespace IPTS.ViewModels
{
    public class PatientProfileViewModel
    {
        public int? Id { get; set; }
        
        // [Display(Name = "Identity Number")]
        // public string IdentityNumber { get; set; } = string.Empty;
        
        [Display(Name = "Birth Date")]
        public DateTime BirthDate { get; set; }
        
        public string? UserId { get; set; }
    }
}
