using IPTS.Models.Entites;

namespace IPTS.ViewModels
{
   public class AppointmentDetailsViewModel
    {
        public int Id { get; set; }

        // Patient
        public string PatientName { get; set; } = string.Empty;
        public string PatientIdentityNumber { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;

        // Doctor
        public string DoctorName { get; set; } = string.Empty;
        public int DoctorId { get; set; }

        // Time
        public DateTime ScheduledTime { get; set; }

        // Status
        public AppointmentStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();

        public string Notes { get; set; } = string.Empty;

        // Computed properties for time slots (20 دقيقة لكل سلوت)
        public int StartSlotIndex { get; set; }
        public int EndSlotIndex { get; set; }
        public int TotalSlots => (EndSlotIndex - StartSlotIndex) + 1;
        public int TotalDurationMinutes => TotalSlots * 20;
        public string TimeRange => $"{StartSlotIndex * 20} - {(EndSlotIndex + 1) * 20} minutes";
    }
}
