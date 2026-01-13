using System.ComponentModel.DataAnnotations;

namespace IPTS.Areas.Admin.ViewsModels
{
    public class PatientFormViewModel
    {
        public int? Id { get; set; }
        
        [Required(ErrorMessage = "Identity Number is required")]
        [StringLength(50, ErrorMessage = "Identity Number cannot exceed 50 characters")]
        [Display(Name = "Identity Number")]
        public string IdentityNumber { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Birth Date is required")]
        [Display(Name = "Birth Date")]
        public DateTime BirthDate { get; set; }
        
        public string? UserId { get; set; }
    }
}
