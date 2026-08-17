using System.ComponentModel.DataAnnotations;
using IPTS.Models.Enums;

namespace IPTS.ViewModels
{
    public class PatientCreateViewModel : IPatientHealthFields
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "First Name is required")]
        [StringLength(50, ErrorMessage = "First Name cannot exceed 50 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required")]
        [StringLength(50, ErrorMessage = "Last Name cannot exceed 50 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // [Required(ErrorMessage = "Identity Number is required")]
        // [StringLength(50, ErrorMessage = "Identity Number cannot exceed 50 characters")]
        // [Display(Name = "Identity Number")]
        public string IdentityNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Birth Date is required")]
        [Display(Name = "Birth Date")]
        public DateTime BirthDate { get; set; }

        public float? Weight { get; set; }
        public float? Height { get; set; }
        public EnBloodGroup? BloodGroup { get; set; }
        public bool? IsSmoker { get; set; }
        public bool? HasChronicDisease { get; set; }
    }
}
