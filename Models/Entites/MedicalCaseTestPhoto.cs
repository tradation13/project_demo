namespace IPTS.Models.Entites
{
    public class MedicalCaseTestPhoto
    {
        public int Id { get; set; }

        public int MedicalCaseId { get; set; }
        public MedicalCase MedicalCase { get; set; } = null!;

        public int TestId { get; set; }
        public Test Test { get; set; } = null!;

        /// <summary>
        /// 0 = Initial (before), 1 = Final (after). Stored as int, not a lookup table.
        /// </summary>
        public int PhotoKind { get; set; }

        /// <summary>
        /// 1 or 2 — two photos per kind.
        /// </summary>
        public int Slot { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
