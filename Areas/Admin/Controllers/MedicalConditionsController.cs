using IPTS.Areas.Admin.ViewsModels;
using IPTS.Helpers;
using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.Resources;
using IPTS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;

namespace IPTS.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = "admin")]
    public class MedicalConditionsController(
        MedicalConditionService medicalConditionService,
        MedicalCaseService medicalCaseService,
        LocService locService,
        AuditService auditService) : Controller
    {
        private readonly MedicalConditionService _medicalConditionService = medicalConditionService;
        private readonly MedicalCaseService _medicalCaseService = medicalCaseService;
        private readonly LocService _locService = locService;
        private readonly AuditService _auditService = auditService;

        public async Task<IActionResult> Index()
        {
            var conditions = await _medicalConditionService.GetAllAsync<MedicalConditionViewModel>(q => q.OrderBy(c => c.Name));

            LogHelper.LogWithContext("Viewed medical conditions list", User?.Identity?.Name ?? "Unknown", "Admin", "MedicalConditionsController.Index", LogEventLevel.Information);

            return View(conditions);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new MedicalConditionViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(MedicalConditionViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var created = await _medicalConditionService.AddAsync(model);

            await _auditService.WriteAsync(
                EnAuditAction.EntityCreated,
                $"Admin created medical condition '{created.Name}'",
                actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                actorUserName: User.Identity?.Name,
                entityName: nameof(MedicalCondition),
                entityId: created.Id?.ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            LogHelper.LogWithContext($"Created medical condition {model.Name}", User?.Identity?.Name ?? "Unknown", "Admin", "MedicalConditionsController.Create", LogEventLevel.Warning);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var condition = await _medicalConditionService.GetByIdAsync(id);
            if (condition == null) return NotFound();

            return View(new MedicalConditionViewModel
            {
                Id = condition.Id,
                Name = condition.Name
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MedicalConditionViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!model.Id.HasValue)
                return BadRequest();

            var updated = await _medicalConditionService.UpdateAsync(model.Id.Value, model);
            if (updated == null)
                return NotFound();

            await _auditService.WriteAsync(
                EnAuditAction.EntityUpdated,
                $"Admin updated medical condition '{updated.Name}'",
                actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                actorUserName: User.Identity?.Name,
                entityName: nameof(MedicalCondition),
                entityId: model.Id?.ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            LogHelper.LogWithContext($"Edited medical condition {model.Name}", User?.Identity?.Name ?? "Unknown", "Admin", "MedicalConditionsController.Edit", LogEventLevel.Warning);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var condition = await _medicalConditionService.GetByIdAsync(id);
            if (condition == null)
            {
                LogHelper.LogWithContext($"Attempted to delete non-existing medical condition with ID {id}", User?.Identity?.Name ?? "Unknown", "Admin", "MedicalConditionsController.Delete", LogEventLevel.Error);
                return NotFound();
            }

            if (await _medicalCaseService.IsExistAsync(c => c.MedicalConditionId == id))
            {
                TempData["ErrorMessage"] = _locService.GetSystem("MedicalCondition_InUse");
                return RedirectToAction("Index");
            }

            await _medicalConditionService.DeleteAsync(id);

            await _auditService.WriteAsync(
                EnAuditAction.EntityDeleted,
                $"Admin deleted medical condition '{condition.Name}'",
                actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                actorUserName: User.Identity?.Name,
                entityName: nameof(MedicalCondition),
                entityId: id.ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            LogHelper.LogWithContext($"Deleted medical condition {condition.Name}", User?.Identity?.Name ?? "Unknown", "Admin", "MedicalConditionsController.Delete", LogEventLevel.Warning);

            return RedirectToAction("Index");
        }
    }
}
