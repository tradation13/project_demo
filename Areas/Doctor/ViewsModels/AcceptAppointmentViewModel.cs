using IPTS.ViewModels;

namespace IPTS.Areas.Doctor.ViewsModels
{
    public class AcceptAppointmentViewModel
    {
        public int AppointmentId { get; set; }
        public int DoctorId { get; set; }
        public string PatientName { get; set; }
        public string PatientEmail { get; set; }
        public DateTime ScheduledDate { get; set; }
        public List<AppointmentTimeSlotViewModel> AvailableSlots { get; set; } = new();
        public List<int> SelectedSlots { get; set; } = new();
        
        // Patient's original selection
        public int StartSlotIndex { get; set; }
        public int EndSlotIndex { get; set; }
        public int TotalDurationMinutes { get; set; }
    }
}
