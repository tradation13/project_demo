using IPTS.ViewModels;

namespace IPTS.Areas.Doctor.ViewsModels
{
    public class AppointmentListViewModel
    {
        public List<AppointmentViewModel> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        // Filters
        public string? PatientName { get; set; }
        public string? Status { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
    }
}