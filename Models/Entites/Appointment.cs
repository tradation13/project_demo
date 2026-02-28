namespace IPTS.Models.Entites
{
    public enum AppointmentStatus
    {
        Pending,
        Confirmed,
        Completed,
        Cancelled
    }

    public class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }
        public string Notes { get; set; } = string.Empty;
        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; }
        public DateTime ScheduledTime { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
        
        // Time slot properties for appointment duration
        public int StartSlotIndex { get; set; }
        public int EndSlotIndex { get; set; }

        // Prescription file storage
        public string? PrescriptionFileName { get; set; } = null;
    }

}
