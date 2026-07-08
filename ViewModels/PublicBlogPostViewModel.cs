namespace IPTS.ViewModels
{
    public class PublicBlogPostViewModel
    {
        public int Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string TitleDe { get; set; } = string.Empty;
        public string TitleEn { get; set; } = string.Empty;
        public string ExcerptDe { get; set; } = string.Empty;
        public string ExcerptEn { get; set; } = string.Empty;
        public string BodyDe { get; set; } = string.Empty;
        public string BodyEn { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
        public string ImageUrl { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new();
    }
}
