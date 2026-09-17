namespace IPTS.Models.Entites
{
    public class MedicalCondition
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<MedicalCase> MedicalCases { get; set; } = new List<MedicalCase>();
    }
}
