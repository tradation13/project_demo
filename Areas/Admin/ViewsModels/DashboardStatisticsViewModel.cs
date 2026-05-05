namespace IPTS.Areas.Admin.ViewsModels
{
    public class DashboardStatisticsViewModel
    {
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalMedicalCases { get; set; }
        public int TotalTests { get; set; }
        public int TotalTestGroups { get; set; }
        public int TotalUserTypes { get; set; }
    }
}