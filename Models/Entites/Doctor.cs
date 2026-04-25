using IPTS.Models.Entites;

namespace IPTS.Models.Entites
{
    public class Doctor
    {
        public int Id { get; set; } 
        public string UserId { get; set; } 
        public AppUser User { get; set; }

        public int SpecialtyId { get; set; }
        public Specialty Specialty { get; set; }
        public ICollection<Appointment> Appointments { get; set; }
        public ICollection<MedicalCase> MedicalCases { get; set; }

        // اسم أو مسار صورة الطبيب
        public string? PhotoUrl { get; set; }
    }
}
