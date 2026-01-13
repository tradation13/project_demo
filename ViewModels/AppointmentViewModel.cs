using IPTS.Models.Entites;

namespace IPTS.ViewModels
{
    public class AppointmentViewModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientIdentityNumber { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public int DoctorId { get; set; }
        public DateTime ScheduledTime { get; set; }
        public AppointmentStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public string Notes { get; set; } = string.Empty;
        
        // Computed properties for time slots
        public int StartSlotIndex { get; set; }
        public int EndSlotIndex { get; set; }
        public int TotalSlots => (EndSlotIndex - StartSlotIndex) + 1;
        public int TotalDurationMinutes => TotalSlots * 20;
        public string TimeRange => $"{StartSlotIndex * 20} - {(EndSlotIndex + 1) * 20} minutes";
    }

    public class AppointmentCreateViewModel
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string ScheduledTime { get; set; } = string.Empty;
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
        public string? Notes { get; set; } = string.Empty;
        
        // Time slot properties
        public int StartSlotIndex { get; set; }
        public int EndSlotIndex { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        
        // Computed properties
        public int TotalSlots => (EndSlotIndex - StartSlotIndex) + 1;
        public int TotalDurationMinutes => TotalSlots * 20;
    }
    public class SingleAppointmentCreateViewModel
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }

        // ÇáÊÇÑíÎ ÇáãÎÊÇÑ (íæã ÝÞØ). ÎÒøäå ßÜ UTC Ýí ÇáßæäÊÑæáÑ ÞÈá ÇáÍÝÙ.
        public DateTime ScheduledDate { get; set; }

        // ÎÇäÉ æÇÍÏÉ ÝÞØ
        public int SlotIndex { get; set; }               // ÈÏíá StartSlotIndex
        public string Time { get; set; } = string.Empty; // ÈÏíá StartTime (ÕíÛÉ ÚÑÖíÉ ãËá "10:20")

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
        public string? Notes { get; set; } = string.Empty;

        // ãÍÓæÈÇÊ ËÇÈÊÉ áãæÚÏ æÇÍÏ
        public int TotalSlots => 1;
        public int TotalDurationMinutes => 20;
    }
    public class AppointmentEditViewModel
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime ScheduledTime { get; set; }
        public AppointmentStatus Status { get; set; }
        public string Notes { get; set; } = string.Empty;
        
        // Time slot properties
        public int StartSlotIndex { get; set; }
        public int EndSlotIndex { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }

    public class PatientSearchViewModel
    {
        public string SearchTerm { get; set; } = string.Empty; // Identity, Phone, or Email
        public string SearchType { get; set; } = "Identity"; // Identity, Phone, Email
    }

    public class AppointmentTimeSlotViewModel
    {
        public string Time { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public bool IsSelected { get; set; }
        public int SlotIndex { get; set; }
    }

    public class AppointmentScheduleViewModel
    {
        public DateTime SelectedDate { get; set; }
        public List<AppointmentTimeSlotViewModel> TimeSlots { get; set; } = new();
        public int TotalDurationMinutes { get; set; }
        public int SelectedSlotsCount { get; set; }
    }
}
