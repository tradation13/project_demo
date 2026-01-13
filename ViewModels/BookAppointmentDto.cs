namespace IPTS.ViewModels
{
    public class BookAppointmentDto
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime TimeSlot { get; set; }
    }
}
