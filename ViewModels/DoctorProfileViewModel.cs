namespace IPTS.ViewModels
{
    public class DoctorProfileViewModel
    {
        public int? Id { get; set; }
        public int SpecialtyId { get; set; }
        public string? UserId { get; set; }

        // اسم الصورة الحالي
        public string? PhotoUrl { get; set; }
        // الصورة الجديدة عند التعديل
        public IFormFile? PhotoFile { get; set; }

        public string? BioDe { get; set; }
        public string? BioEn { get; set; }
    }
}
