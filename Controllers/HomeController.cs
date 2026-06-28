using IPTS.Models;
using IPTS.Resources;
using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace IPTS.Controllers
{
    public class HomeController(LocService locService, UserService userService, EmailService emailService, IConfiguration configuration) : Controller
    {
        private readonly LocService _locService = locService;
        private readonly UserService _userService = userService;
        private readonly EmailService _emailService = emailService;
        private readonly IConfiguration _configuration = configuration;

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

         [HttpGet]
        public IActionResult Treatments()
        {
            return View();
        }

           [HttpGet]
        public IActionResult OurProjects()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

 
         
            var subject = $"{_locService.GetSystem("Notification_NewContact")} {model.Name ?? _locService.GetSystem("Label_Visitor")}";
            var body = $@"<p><strong>{_locService.GetSystem("Label_Name")}:</strong> {model.Name}</p>
                        <p><strong>{_locService.GetSystem("Label_Email")}:</strong> {model.Email}</p>
                        <p><strong>{_locService.GetSystem("Label_Phone")}:</strong> {model.Phone}</p>
                        <p><strong>{_locService.GetSystem("Label_Message")}:</strong><br/>{model.Message}</p>";

            try
            {
                await _emailService.SendEmail("dr.kurtoglu@physiotech-ehrenfeld.de", subject, body, model.Email);
                TempData["ContactSuccess"] = _locService.GetSystem("Status_SuccessSent");
                return RedirectToAction(nameof(Contact));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, _locService.GetSystem("Status_ErrorGeneral"));
                Debug.WriteLine(ex);
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Therapies(string search = "", string sort = "name", string specialty = "")
        {
            var therapies = await _userService.GetAllAsync<DoctorViewModel>(u => u.Include(u => u.Doctor).Where(u => u.Doctor != null && u.Status == Models.Enums.EnUserStatus.Active));

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

 [HttpGet]
public IActionResult SetLanguage(string culture, string returnUrl)
{
    // 1. تثبيت اللغة في الكوكيز
    Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
    );

    // 2. فحص الرابط (المريض يواجه مشكلة مع الروابط التي تحتوي على سبيس أو كاراكتر خاص)
    if (string.IsNullOrEmpty(returnUrl))
    {
        returnUrl = Request.Headers["Referer"].ToString();
    }

    // 3. الأمان: إذا كان الرابط خارجي أو فارغ ارجع للرئيسية
    if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
    {
        return Redirect("/");
    }

    // 4. الحل السحري لمشكلة الـ 404: 
    // نستخدم Redirect بدل LocalRedirect لأنها تتعامل مع الـ Routes المعقدة بشكل أفضل
    return Redirect(returnUrl); 
}


    }
}
