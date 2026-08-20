namespace IPTS.ViewModels
{
    public class GuestRegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public bool AcceptPrivacy { get; set; }
        public bool AcceptTerms { get; set; }
        public bool AcceptHealthDataConsent { get; set; }
        public string? DoctorUserId { get; set; }
    }
}
