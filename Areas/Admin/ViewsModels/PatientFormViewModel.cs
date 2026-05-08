using System.ComponentModel.DataAnnotations;

namespace IPTS.Areas.Admin.ViewsModels
{
    public class PatientFormViewModel
    {
        public int? Id { get; set; }
        
        public string IdentityNumber { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Birth Date is required")]
        [Display(Name = "Birth Date")]
        public DateTime BirthDate { get; set; }
        
        public string? UserId { get; set; }
    }
}
