using System.ComponentModel.DataAnnotations;

namespace IPTS.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
