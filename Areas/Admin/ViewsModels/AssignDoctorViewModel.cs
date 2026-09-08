namespace IPTS.Areas.Admin.ViewsModels
{
    public class AssignDoctorViewModel
    {
        public int PatientId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public int AssignedDoctorId { get; set; }
    }
}
