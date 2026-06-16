namespace IPTS.Areas.Doctor.ViewsModels
{
    public class DoctorDashboardStatsViewModel
    {
        // Core KPIs
        public int TotalAppointments { get; set; }
        public int UpcomingAppointments { get; set; }
        public int CompletedThisMonth { get; set; }
        public int CancelledThisMonth { get; set; }

        // Patients & Cases
        public int UniquePatientsAllTime { get; set; }
        public int ActiveMedicalCases { get; set; }
        public int TestsOrderedThisMonth { get; set; }

        // Time & Efficiency
        public double CompletionRateThisMonthPercent { get; set; } // 0-100
        public double AvgAppointmentsPerDayLast7Days { get; set; }

        // Convenience
        public DateTime? NextAppointmentUtc { get; set; }
        // Latest pending appointment requests (up to 3)
        public List<IPTS.ViewModels.AppointmentViewModel> LatestPendingRequests { get; set; } = new();
    }

}
