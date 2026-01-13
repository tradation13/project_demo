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

        public ICollection<MedicalCaseTest> MedicalCaseTests { get; set; }
    }

}
