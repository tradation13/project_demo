namespace IPTS.Models.Entites
{
    public class BlogPostImage
    {
        public int Id { get; set; }
        public int BlogPostId { get; set; }
        public BlogPost? BlogPost { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public bool IsMain { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
