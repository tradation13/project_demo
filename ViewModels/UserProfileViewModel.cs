using IPTS.Models.Enums;
using IPTS.ViewModels;

namespace IPTS.ViewModels
{
    public class UserProfileViewModel
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string UserName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        // بيانات خاصة بالحساب
        public AdminProfileViewModel? Admin { get; set; }
        public CustomerProfileViewModel? Customer { get; set; }
        public PatientProfileViewModel? Patient { get; set; }   // جديد
        public DoctorProfileViewModel? Doctor { get; set; }
    }
}
