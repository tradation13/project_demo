namespace IPTS.Areas.Admin.ViewsModels
{
    public class DoctorFormViewModel
    {
        public int? Id { get; set; }
        public int SpecialtyId { get; set; }
        public string? UserId { get; set; }

        // خاصية رفع الصورة
        public IFormFile? PhotoFile { get; set; }
        // اسم أو مسار الصورة المحفوظة
        public string? PhotoUrl { get; set; }
    }
}
