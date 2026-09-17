namespace IPTS.Models.Entites
{
    public class TestParameter
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public Test Test { get; set; } = null!;
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public DateTime? Date { get; set; }
    }
}
