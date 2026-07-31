using IPTS.Data;
using IPTS.Models.Entites;
using IPTS.Resources;
using IPTS.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace IPTS.Services
{
    public class BlogPostService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly LocService _locService;
        private readonly string _storagePath;
        private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        private const long MaxFileSize = 5 * 1024 * 1024;

        public BlogPostService(ApplicationDbContext context, IWebHostEnvironment env, LocService locService)
        {
            _context = context;
            _env = env;
            _locService = locService;
            _storagePath = Path.Combine(env.ContentRootPath, "InternalStorage", "BlogsImages");
            if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
        }

        public async Task<List<BlogPost>> GetAllAsync(bool includeImages = true)
        {
            var query = _context.BlogPosts.AsQueryable();
            if (includeImages)
            {
                query = query.Include(x => x.Images).Include(x => x.CreatedByUser);
            }

            return await query.OrderByDescending(x => x.PublishedAt).ThenByDescending(x => x.Id).ToListAsync();
        }

        public async Task<List<BlogPost>> GetHomeAddPostsAsync(int take = 3)
        {
            return await _context.BlogPosts
                .AsNoTracking()
                .Include(x => x.Images)
                .Where(x => x.IsPublished && x.PostType == EnBlogPostType.Add)
                .OrderByDescending(x => x.PublishedAt)
                .ThenByDescending(x => x.Id)
                .Take(take)
                .ToListAsync();
        }

        public async Task<BlogPost?> GetByIdAsync(int id)
        {
            return await _context.BlogPosts
                .Include(x => x.Images)
                .Include(x => x.CreatedByUser)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<BlogPost> CreateAsync(BlogPost entity, string? createdByUserId)
        {
            entity.CreatedByUserId = createdByUserId;
            entity.CreatedAt = DateTime.UtcNow;
            entity.PublishedAt = NormalizeToUtc(entity.PublishedAt == default ? DateTime.UtcNow : entity.PublishedAt);
            entity.Slug = string.IsNullOrWhiteSpace(entity.Slug) ? GenerateSlug(entity.Title) : GenerateSlug(entity.Slug);

            await _context.BlogPosts.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<BlogPost?> UpdateAsync(BlogPost entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            entity.PublishedAt = NormalizeToUtc(entity.PublishedAt == default ? DateTime.UtcNow : entity.PublishedAt);
            entity.Slug = string.IsNullOrWhiteSpace(entity.Slug) ? GenerateSlug(entity.Title) : GenerateSlug(entity.Slug);
            _context.BlogPosts.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        private static DateTime NormalizeToUtc(DateTime dateTime)
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime()
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.BlogPosts
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) return false;

            foreach (var image in entity.Images)
            {
                DeletePhysicalFile(image.FileName);
            }

            _context.BlogPosts.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IReadOnlyList<BlogPostImage>> SaveImagesAsync(int blogPostId, IEnumerable<IFormFile>? files, bool setFirstAsMain = true)
        {
            if (files == null) return new List<BlogPostImage>();

            var images = new List<BlogPostImage>();
            var index = 0;

            foreach (var file in files.Where(x => x != null && x.Length > 0))
            {
                var validation = ValidateImage(file);
                if (!validation.IsValid) continue;

                var fileName = await SaveFileAsync(file);
                if (string.IsNullOrWhiteSpace(fileName)) continue;

                var entity = new BlogPostImage
                {
                    BlogPostId = blogPostId,
                    FileName = fileName,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    DisplayOrder = index,
                    IsMain = setFirstAsMain && index == 0
                };

                _context.BlogPostImages.Add(entity);
                images.Add(entity);
                index++;
            }

            await _context.SaveChangesAsync();
            return images;
        }

        public async Task<bool> RemoveImageAsync(int imageId)
        {
            var image = await _context.BlogPostImages.FirstOrDefaultAsync(x => x.Id == imageId);
            if (image == null) return false;

            DeletePhysicalFile(image.FileName);
            _context.BlogPostImages.Remove(image);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<BlogPost>> GetPagedAsync(int page, int pageSize = 10)
        {
            return await _context.BlogPosts
                .AsNoTracking()
                .Include(x => x.Images)
                .OrderByDescending(x => x.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync() => await _context.BlogPosts.CountAsync();

        private async Task<string?> SaveFileAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension)) return null;

            var safeName = CleanFileName(Path.GetFileNameWithoutExtension(file.FileName));
            var fileName = $"{safeName}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(_storagePath, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return fileName;
        }

        private void DeletePhysicalFile(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;
            var filePath = Path.Combine(_storagePath, fileName);
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        private (bool IsValid, string Error) ValidateImage(IFormFile file)
        {
            if (file == null || file.Length == 0) return (false, _locService.GetSystem("File_Empty"));
            if (file.Length > MaxFileSize) return (false, _locService.GetSystem("File_TooLarge"));
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedExtensions.Contains(extension) ? (true, string.Empty) : (false, _locService.GetSystem("File_InvalidExtension"));
        }

        private string CleanFileName(string fileName)
        {
            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var invalidRegex = new Regex($"[{invalidChars}]");
            var cleaned = invalidRegex.Replace(fileName, string.Empty);
            cleaned = Regex.Replace(cleaned, @"\s+", "_");
            return string.IsNullOrWhiteSpace(cleaned) ? "blog" : cleaned;
        }

        public string GenerateSlug(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "blog-post";
            var slug = text.ToLowerInvariant();
            slug = slug.Replace("&", "-and-");
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            return slug.Trim('-');
        }

        public string GetImageUrl(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return string.Empty;
            return $"/InternalStorage/BlogsImages/{fileName}";
        }
    }
}
