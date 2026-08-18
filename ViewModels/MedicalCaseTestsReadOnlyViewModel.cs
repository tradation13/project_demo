using IPTS.Models.Entites;

namespace IPTS.ViewModels
{
    public class MedicalCaseTestsReadOnlyViewModel
    {
        public MedicalCase Case { get; set; } = null!;
        public string Area { get; set; } = "admin";
    }
}
