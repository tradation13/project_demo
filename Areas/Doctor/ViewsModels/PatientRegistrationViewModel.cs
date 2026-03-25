namespace IPTS.Areas.Doctor.ViewsModels
{       
    public class PatientRegistrationViewModel
    {
        public required string UserName { get; set; }

        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        // public required string NationalId { get; set; }

        public required string PhoneNumber { get; set; }

        public required string Email { get; set; }

        public DateTime DateOfBirth { get; set; }
    }
}