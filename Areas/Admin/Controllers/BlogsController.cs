using AutoMapper;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Data;
using IPTS.Models.Entites;
using IPTS.Resources;
using IPTS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IPTS.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = "admin")]
    public class BlogsController(BlogPostService blogPostService, IMapper mapper, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, LocService locService) : Controller
    {
        private readonly BlogPostService _blogPostService = blogPostService;
        private readonly IMapper _mapper = mapper;
        private readonly ApplicationDbContext _context = context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly LocService _locService = locService;

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var totalCount = await _blogPostService.CountAsync();
            var posts = await _blogPostService.GetPagedAsync(page, pageSize);
            var model = new BlogPostListViewModel
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Posts = _mapper.Map<List<BlogPostViewModel>>(posts)
            };
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var post = await _blogPostService.GetByIdAsync(id);
            if (post == null) return NotFound();
            var model = _mapper.Map<BlogPostViewModel>(post);
            return View(model);
        }

        public IActionResult Create() => View(new BlogPostViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPostViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var entity = _mapper.Map<BlogPost>(model);
            entity.MainImagePath = model.Images.FirstOrDefault()?.Url;
            var created = await _blogPostService.CreateAsync(entity, _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            if (model.Files != null && model.Files.Any())
            {
                await _blogPostService.SaveImagesAsync(created.Id, model.Files);
            }

            TempData["SuccessMessage"] = _locService.GetSystem("Blog_Create_Success");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var post = await _blogPostService.GetByIdAsync(id);
            if (post == null) return NotFound();
            var model = _mapper.Map<BlogPostViewModel>(post);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPostViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var post = await _blogPostService.GetByIdAsync(id);
            if (post == null) return NotFound();

            _mapper.Map(model, post);
            post.MainImagePath = post.Images.FirstOrDefault(x => x.IsMain)?.FileName;
            await _blogPostService.UpdateAsync(post);

            if (model.Files != null && model.Files.Any())
            {
                await _blogPostService.SaveImagesAsync(post.Id, model.Files);
            }

            TempData["SuccessMessage"] = _locService.GetSystem("Blog_Update_Success");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _blogPostService.DeleteAsync(id);
            TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted ? _locService.GetSystem("Blog_Delete_Success") : _locService.GetSystem("Blog_Delete_NotFound");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId, int blogPostId)
        {
            var deleted = await _blogPostService.RemoveImageAsync(imageId);
            TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted ? _locService.GetSystem("Blog_Image_Delete_Success") : _locService.GetSystem("Blog_Image_Delete_Failed");
            return RedirectToAction(nameof(Edit), new { id = blogPostId });
        }
    }

    public class BlogPostListViewModel
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public List<BlogPostViewModel> Posts { get; set; } = new();
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
