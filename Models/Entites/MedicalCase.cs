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

        public EnDominantSide? DominantSide { get; set; }
        public EnActivityLevel? ActivityLevel { get; set; }

        // --- معلومات إضافية لتحسين جودة التقرير --- //
        /// <summary>
        /// تاريخ بداية الإصابة الحالية، وهل كان السبب حادثاً مفاجئاً أم ظهرت تدريجياً
        /// </summary>
        public string InjuryHistory { get; set; }

        /// <summary>
        /// قائمة الأدوية التي يتناولها المريض
        /// </summary>
        public string Medications { get; set; }

        /// <summary>
        /// كيف تؤثر الإصابة على الحياة اليومية (المشي، صعود الدرج، النوم، العمل...)
        /// </summary>
        public string FunctionalAbility { get; set; }

        /// <summary>
        /// الأهداف الشخصية للمريض (مثلاً: العودة للركض، أو حمل الطفل بدون ألم)
        /// </summary>
        public string PersonalGoals { get; set; }

        public ICollection<MedicalCaseTest> MedicalCaseTests { get; set; }
        public ICollection<MedicalReportHistory> MedicalReportHistories { get; set; }
    }

}
