using IPTS.Models.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace IPTS.Areas.Admin.ViewsModels
{
    public class BlogPostViewModel
    {
        public int Id { get; set; }

        
        public string Title { get; set; } = string.Empty;

       
        public string? Slug { get; set; }

        
        public string ShortDescription { get; set; } = string.Empty;

                public string LongDescription { get; set; } = string.Empty;

        
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        
        public EnBlogPostType PostType { get; set; } = EnBlogPostType.News;

        
        public bool IsPublished { get; set; } = true;

        
        public bool IsFeatured { get; set; }

        public string? MainImagePath { get; set; }
        public string? CreatedByUserId { get; set; }
        public List<BlogPostImageViewModel> Images { get; set; } = new();
        public List<IFormFile>? Files { get; set; }

        public bool IsNewPost => Id == 0;
    }

    public class BlogPostImageViewModel
    {
        public int Id { get; set; }
        public int BlogPostId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public bool IsMain { get; set; }
        public int DisplayOrder { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
