using IPTS.Models.Enums;
using IPTS.ViewModels;

namespace IPTS.Areas.Doctor.ViewsModels
{       
    public class PatientRegistrationViewModel : IPatientHealthFields
    {
        public required string UserName { get; set; }

        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        // public required string NationalId { get; set; }

        public required string PhoneNumber { get; set; }

        public required string Email { get; set; }

        public DateTime DateOfBirth { get; set; }

        public float? Weight { get; set; }
        public float? Height { get; set; }
        public EnBloodGroup? BloodGroup { get; set; }
        public bool? IsSmoker { get; set; }
        public bool? HasChronicDisease { get; set; }
    }
}