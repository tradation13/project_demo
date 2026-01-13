using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> Logs(string? systemSection)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var (hasDashboard, logs) = await _systemService.GetLogsAsync(userId, systemSection);

            if (!hasDashboard)
                return Unauthorized();

            ViewBag.HasDashboard = hasDashboard;
            return View("Logs", logs);
        }
    }
}
