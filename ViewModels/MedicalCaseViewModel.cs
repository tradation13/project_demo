using IPTS.Models.Enums; // استيراد الـ Enums الجديدة

namespace IPTS.ViewModels
{
    public class MedicalCaseViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; 
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int DoctorId { get; set; }
        public int PatientId { get; set; }

        // --- Physical Vitals (البلوك الأزرق) ---
        
        public float? Weight { get; set; } // الوزن بالكيلو
        public float? Height { get; set; } // الطول بالسنتيمتر
        
        // استخدام الـ Enums المخصصة
        public EnDominantSide? DominantSide { get; set; }
        public EnBloodGroup? BloodGroup { get; set; }
        public EnActivityLevel? ActivityLevel { get; set; }

        // حقول نعم/لا (Checkboxes)
      public bool IsSmoker { get; set; } = false;
public bool HasChronicDisease { get; set; } = false;

        public List<MedicalCaseTestViewModel> Tests { get; set; } = new();
    }
}