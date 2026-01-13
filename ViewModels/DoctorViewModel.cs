namespace IPTS.ViewModels
{
    public class DoctorViewModel
    {
        public  int Id { get; set; }
        public  string UserId { get; set; }
        public string FullName { get; set; }
        public string Specialty { get; set; }
        public string? PhotoUrl { get; set; }
        public double? Rating { get; set; }
        public int? YearsOfExperience { get; set; }
        public bool? IsAvailable { get; set; }
    }
}
