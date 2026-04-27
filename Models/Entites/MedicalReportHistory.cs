
namespace IPTS.Models.Entites
{
    public class MedicalReportHistory
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int MedicalCaseId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ReportUrl { get; set; } // محتوى التقرير الطبي
        public AppUser? User {  get; set; }
        public MedicalCase MedicalCase { get; set; }
    }
}
