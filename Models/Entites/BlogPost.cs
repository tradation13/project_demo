using IPTS.Models.Enums;

namespace IPTS.Models.Entites
{
    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string LongDescription { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public EnBlogPostType PostType { get; set; } = EnBlogPostType.News;
        public bool IsPublished { get; set; } = true;
        public bool IsFeatured { get; set; }
        public string? MainImagePath { get; set; }
        public string? CreatedByUserId { get; set; }
        public AppUser? CreatedByUser { get; set; }
        public List<BlogPostImage> Images { get; set; } = new();
    }
}
