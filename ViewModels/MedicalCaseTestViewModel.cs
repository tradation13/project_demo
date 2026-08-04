namespace IPTS.ViewModels
{
    public class MedicalCaseTestViewModel
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public int MedicalCaseId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string TestGroupName { get; set; } = string.Empty;
        public string? Result { get; set; }
        public decimal? StandardValue { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
