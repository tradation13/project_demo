namespace IPTS.Areas.Admin.ViewsModels
{
    public class TestParameterViewModel
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public DateTime? Date { get; set; }
    }
}
