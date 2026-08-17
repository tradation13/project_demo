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

        public EnDominantSide? DominantSide { get; set; }
        public EnActivityLevel? ActivityLevel { get; set; }

        public List<MedicalCaseTestViewModel> Tests { get; set; } = new();
        
        // --- معلومات إضافية لتحسين جودة التقرير --- //
        /// <summary>
        /// تاريخ بداية الإصابة الحالية، وهل كان السبب حادثاً مفاجئاً أم ظهرت تدريجياً
        /// </summary>
        public string InjuryHistory { get; set; } = string.Empty;

        /// <summary>
        /// قائمة الأدوية التي يتناولها المريض
        /// </summary>
        public string Medications { get; set; } = string.Empty;

        /// <summary>
        /// كيف تؤثر الإصابة على الحياة اليومية (المشي، صعود الدرج، النوم، العمل...)
        /// </summary>
        public string FunctionalAbility { get; set; } = string.Empty;

        /// <summary>
        /// الأهداف الشخصية للمريض (مثلاً: العودة للركض، أو حمل الطفل بدون ألم)
        /// </summary>
        public string PersonalGoals { get; set; } = string.Empty;
    }
}