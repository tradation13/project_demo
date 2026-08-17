using IPTS.Models.Entites;

namespace IPTS.ViewModels
{
    public class TestPhotoCompareViewModel
    {
        public int MedicalCaseId { get; set; }
        public int TestId { get; set; }
        public string Area { get; set; } = "doctor";
        public bool CanEdit { get; set; }
        public IReadOnlyList<MedicalCaseTestPhoto> Photos { get; set; } = [];
    }
}
