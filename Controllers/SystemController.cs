using IPTS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPTS.Controllers
{
    public class SystemController(SystemService systemService) : Controller
    {
        private readonly SystemService _systemService = systemService;

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SystemSettings()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult Logs()
        {
            return RedirectToAction("Index", "Logs", new { area = "admin" });
        }
    }
}
