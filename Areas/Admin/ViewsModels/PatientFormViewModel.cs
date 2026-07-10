using System.ComponentModel.DataAnnotations;

namespace IPTS.Areas.Admin.ViewsModels
{
    public class PatientFormViewModel
    {
        public int? Id { get; set; }
        
        public string IdentityNumber { get; set; } = string.Empty;
        
        [Display(Name = "Label_BirthDate")]
        public DateTime BirthDate { get; set; }
        
        public string? UserId { get; set; }
    }
}
