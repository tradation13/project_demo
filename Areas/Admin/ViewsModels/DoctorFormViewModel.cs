namespace IPTS.Areas.Admin.ViewsModels
{
    public class DoctorFormViewModel
    {
        public int? Id { get; set; }
        public int SpecialtyId { get; set; }
        public string? UserId { get; set; }

       
        public IFormFile? PhotoFile { get; set; }
        
        public string? PhotoUrl { get; set; }

        public string? BioDe { get; set; }
        public string? BioEn { get; set; }
    }
}
