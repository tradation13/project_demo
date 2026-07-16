using IPTS.Areas.Admin.ViewsModels;
using IPTS.Helpers;
using IPTS.Models.Enums;
using IPTS.Resources;
using IPTS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;
using System.Security.Claims;

namespace IPTS.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = "admin")]
    public class LogsController(
        SystemService systemService,
        AuditService auditService,
        LocService locService) : Controller
    {
        private readonly SystemService _systemService = systemService;
        private readonly AuditService _auditService = auditService;
        private readonly LocService _locService = locService;

        [HttpGet]
        public async Task<IActionResult> Index(
            string tab = "logging",
            string? section = null,
            string? level = null,
            DateTime? from = null,
            DateTime? to = null,
            int? action = null,
            string? actor = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            var userName = User.Identity?.Name ?? "Unknown";

            tab = string.Equals(tab, "auditing", StringComparison.OrdinalIgnoreCase)
                ? "auditing"
                : "logging";

            var model = new AdminLogsPageViewModel
            {
                Tab = tab,
                Section = section,
                Level = level,
                From = from,
                To = to,
                Action = action,
                Actor = actor
            };

            try
            {
                if (tab == "logging")
                {
                    var (_, logs) = await _systemService.GetLogsAsync(userId, section, level, from, to);
                    model.Logs = logs;
                }
                else
                {
                    model.Audits = await _auditService.GetAsync(action, actor, from, to);
                }

                LogHelper.LogWithContext(
                    $"Opened admin logs tab '{tab}'",
                    userId,
                    "Admin",
                    "LogsController.Index",
                    LogEventLevel.Information);

                return View(model);
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error loading admin logs: {ex.Message}",
                    userId,
                    "Admin",
                    "LogsController.Index",
                    LogEventLevel.Error);

                TempData["ErrorMessage"] = _locService.GetSystem("Msg_ErrorSave");
                return View(model);
            }
        }
    }
}
