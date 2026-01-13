namespace IPTS.Areas.Doctor.ViewsModels
{
    public class RejectAppointmentViewModel
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; }
        public string PatientEmail { get; set; }
        public string RejectReason { get; set; }
    }

}
