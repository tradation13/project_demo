namespace IPTS.Models.Entites
{
    public class Test
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TestGroupId { get; set; }
        public TestGroup TestGroup { get; set; }
        public List<MedicalCaseTest> MedicalCaseTests { get; set; }
        public List<MedicalCaseTestPhoto> TestPhotos { get; set; } = new();

    }

}
