namespace IPTS.Areas.Admin.ViewsModels
{
    public class TestGroupDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<TestGroupDetailsTestViewModel> Tests { get; set; } = new();
    }

    public class TestGroupDetailsTestViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? StandardValue { get; set; }
        public List<TestGroupDetailsCaseViewModel> Cases { get; set; } = new();
    }

    public class TestGroupDetailsCaseViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
