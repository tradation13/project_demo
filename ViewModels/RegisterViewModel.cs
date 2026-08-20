using IPTS.Areas.Admin.ViewsModels;
using IPTS.ViewModels;

namespace IPTS.ViewModels
{
    public class RegisterViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string UserTypeName { get; set; } = string.Empty;
        
        public bool AcceptPrivacy { get; set; }
        public bool AcceptTerms { get; set; }
        /// <summary>Patient health-data processing consent (Art. 9). Must stay unchecked by default.</summary>
        public bool AcceptHealthDataConsent { get; set; }
        public string? BookingDoctorUserId { get; set; }

        public CustomerRegisterViewModel? Customer { get; set; }
        public PatientRegisterViewModel? Patient { get; set; }
        public DoctorRegisterViewModel? Doctor { get; set; }
    }
}
