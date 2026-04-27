using IPTS.Models.Enums;

namespace IPTS.Models.Entites
{
    public class MedicalCase
    {
        public int Id { get; set; }
        public string Name { get; set; } // اسم المرض أو الحالة
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; }
        public Doctor Doctor { get; set; }
        public int? DoctorId { get; set; }

        // Physical Vitals
    public float? Weight { get; set; }
    public float? Height { get; set; }
    
    public EnDominantSide? DominantSide { get; set; }
    public EnBloodGroup? BloodGroup { get; set; }
    public EnActivityLevel? ActivityLevel { get; set; }

    public bool? IsSmoker { get; set; }
    public bool? HasChronicDisease { get; set; }

        public ICollection<MedicalCaseTest> MedicalCaseTests { get; set; }
        public ICollection<MedicalReportHistory> MedicalReportHistories { get; set; }
    }

}
