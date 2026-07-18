using IPTS.Areas.Admin.ViewsModels;
using IPTS.Models.Entites;
using IPTS.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;
using IPTS.Data;
using IPTS.Services;
using IPTS.Models.Enums;

namespace IPTS.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = "admin")]
    public class TestGroupsController(
        TestGroupService testGroupService,
        AuditService auditService) : Controller
    {
        private readonly TestGroupService _testGroupService = testGroupService;
        private readonly AuditService _auditService = auditService;

        public async Task<IActionResult> Index()
        {
            var groups = await _testGroupService.GetAllAsync<TestGroupViewModel>();

            LogHelper.LogWithContext("Viewed test groups list", User?.Identity?.Name ?? "Unknown", "Admin", "TestGroupsController.Index", LogEventLevel.Information);

            return View(groups);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new TestGroupViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(TestGroupViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var createdGroup = await _testGroupService.AddAsync(model);

            await _auditService.WriteAsync(
                EnAuditAction.EntityCreated,
                $"Admin created test group '{createdGroup.Name}'",
                actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                actorUserName: User.Identity?.Name,
                entityName: nameof(TestGroup),
                entityId: createdGroup.Id?.ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            LogHelper.LogWithContext($"Created test group {model.Name}", User?.Identity?.Name ?? "Unknown", "Admin", "TestGroupsController.Create", LogEventLevel.Warning);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var group = await _testGroupService.GetByIdAsync(id);
            if (group == null) return NotFound();

            var model = new TestGroupViewModel
            {
                Id = group.Id,
                Name = group.Name
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TestGroupViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!model.Id.HasValue)
                return BadRequest();

            var updatedGroup = await _testGroupService.UpdateAsync(model.Id.Value, model);
            if (updatedGroup == null)
                return NotFound();

            await _auditService.WriteAsync(
                EnAuditAction.EntityUpdated,
                $"Admin updated test group '{updatedGroup.Name}'",
                actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                actorUserName: User.Identity?.Name,
                entityName: nameof(TestGroup),
                entityId: model.Id?.ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            LogHelper.LogWithContext($"Edited test group {model.Name}", User?.Identity?.Name ?? "Unknown", "Admin", "TestGroupsController.Edit", LogEventLevel.Warning);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var group = await _testGroupService.GetByIdAsync(id);
            if(group == null)
            {
                LogHelper.LogWithContext($"Attempted to delete non-existing test group with ID {id}", User?.Identity?.Name ?? "Unknown", "Admin", "TestGroupsController.Delete", LogEventLevel.Error);
                return NotFound();
            }

            await _testGroupService.DeleteAsync(id);

            await _auditService.WriteAsync(
                EnAuditAction.EntityDeleted,
                $"Admin deleted test group '{group.Name}'",
                actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                actorUserName: User.Identity?.Name,
                entityName: nameof(TestGroup),
                entityId: id.ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            LogHelper.LogWithContext($"Deleted test group {group.Name}", User?.Identity?.Name ?? "Unknown", "Admin", "TestGroupsController.Delete", LogEventLevel.Warning);

            return RedirectToAction("Index");
        }
    }
}
