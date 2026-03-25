using IPTS.Models.Entites;

namespace IPTS.Models.Entites
{
    public class Patient
    {
        public int Id { get; set; } // نفس AppUser Id
        public string UserId { get; set; }
        public AppUser User { get; set; }

        // public string IdentityNumber { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public ICollection<Appointment> Appointments { get; set; }
        public ICollection<MedicalCase> MedicalCases { get; set; }
    }

}
