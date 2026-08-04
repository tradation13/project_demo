namespace IPTS.Models.Entites
{
    public class MedicalCaseTest
    {
        public int Id { get; set; }

        public int MedicalCaseId { get; set; }
        public MedicalCase MedicalCase { get; set; }
        public DateTime CreatedAt { get; set; }

        public int TestId { get; set; }
        public Test Test { get; set; }

        public string? Result { get; set; }

        /// <summary>
        /// Target/standard value set by the doctor for this test within this case.
        /// </summary>
        public decimal? StandardValue { get; set; }
    }
}


