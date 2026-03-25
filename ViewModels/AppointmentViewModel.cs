using IPTS.Models.Entites;
using Microsoft.AspNetCore.Http;

namespace IPTS.ViewModels
{
    public class AppointmentViewModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        // public string PatientIdentityNumber { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public int DoctorId { get; set; }
        public DateTime ScheduledTime { get; set; }
        public AppointmentStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public string Notes { get; set; } = string.Empty;
        public string? PrescriptionFileName { get; set; }
        
        // Computed property to extract original filename from stored filename
        public string? PrescriptionDisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PrescriptionFileName))
                    return null;
                
                // Extract original name: "originalname_guid.ext" -> "originalname.ext"
                var lastUnderscoreIndex = PrescriptionFileName.LastIndexOf('_');
                if (lastUnderscoreIndex <= 0)
                    return PrescriptionFileName;
                
                var originalName = PrescriptionFileName.Substring(0, lastUnderscoreIndex);
                var extension = System.IO.Path.GetExtension(PrescriptionFileName);
                return originalName + extension;
            }
        }
        
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
        public IFormFile? PrescriptionFile { get; set; }
        public string? PrescriptionFileName { get; set; }
        
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

        // تاريخ الموعد (بدون وقت). يحتفظ بـ UTC بدون تحويل أي منهما.
        public DateTime ScheduledDate { get; set; }

        // حقل واحد فقط بواقت يوم
        public int SlotIndex { get; set; }               // نفس StartSlotIndex
        public string Time { get; set; } = string.Empty; // مثل StartTime (بصيغة تسلسل مثل "10:20")

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
        public string? Notes { get; set; } = string.Empty;

        // Prescription file
        public IFormFile? PrescriptionFile { get; set; }

        // خصائص محسوبة للعرض فقط
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
