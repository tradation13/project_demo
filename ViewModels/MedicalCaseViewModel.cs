namespace IPTS.ViewModels
{
    public class MedicalCaseViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // اسم المرض أو الحالة
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public List<MedicalCaseTestViewModel> Tests { get; set; } = new();
    }
}
