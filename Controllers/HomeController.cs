using IPTS.Models;
using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace IPTS.Controllers
{
    public class HomeController(UserService userService, EmailService emailService, IConfiguration configuration, IMemoryCache cache) : Controller
    {
        private readonly UserService _userService = userService;
        private readonly EmailService _emailService = emailService;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMemoryCache _cache = cache;

        [OutputCache(Duration = 3600)]
        public IActionResult Index()
        {
            return View();
        }

         public IActionResult Privacy()
        {
            return View();
        }
        [OutputCache(Duration = 3600)]
        public IActionResult About()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Contact()
        {
            return View(new ContactFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

         
            var subject = $"New contact from {model.Name ?? "Visitor"}";
            var body = $@"<p><strong>Name:</strong> {model.Name}</p>
                        <p><strong>Email:</strong> {model.Email}</p>
                        <p><strong>Phone:</strong> {model.Phone}</p>
                        <p><strong>Message:</strong><br/>{model.Message}</p>";

            try
            {
                await _emailService.SendEmail("tradation10@gmail.com", subject, body);
                TempData["Success"] = "Thank you! Your message has been sent.";
                return RedirectToAction(nameof(Contact));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Could not send your message. Please try again later.");
                Debug.WriteLine(ex);
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Therapies(string search = "", string sort = "name", string specialty = "")
        {
            // Fetch all doctors
            // Use Cache
            if(!_cache.TryGetValue("therapies", out List<DoctorViewModel> therapies))
            {
                therapies = await _userService.GetAllAsync<DoctorViewModel>(u => u.Include(u => u.Doctor).Where(u => u.Doctor != null && u.Status == Models.Enums.EnUserStatus.Active));

                _cache.Set("therapies", therapies, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1)));
            }

            // Filter by search
            if (!string.IsNullOrWhiteSpace(search))
            {
                therapies = therapies.Where(d =>
                    d.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    d.Specialty.Contains(search, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Filter by specialty
            if (!string.IsNullOrWhiteSpace(specialty))
            {
                therapies = therapies.Where(d => d.Specialty == specialty).ToList();
            }

            // - Sort -
            therapies = sort switch
            {
                "rating" => [.. therapies.OrderByDescending(d => d.Rating)],
                "experience" => [.. therapies.OrderByDescending(d => d.YearsOfExperience)],
                "availability" => [.. therapies.OrderByDescending(d => d.IsAvailable)],
                _ => [.. therapies.OrderBy(d => d.FullName)]
            };

            return View(therapies);
        }

        [HttpGet] // أو HttpGet حسب تفضيلك، يفضل Post للأمان
public IActionResult SetLanguage(string culture, string returnUrl)
{
    Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
    );

    return LocalRedirect(returnUrl);
}



    }
}
