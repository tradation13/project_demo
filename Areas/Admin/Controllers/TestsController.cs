using IPTS.Areas.Admin.ViewsModels;
using IPTS.Models.Entites;
using IPTS.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;
using IPTS.Services;
using Microsoft.EntityFrameworkCore;
using IPTS.Models.Enums;

namespace IPTS.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = "admin")]
    public class TestsController(
        TestService testService,
        TestGroupService testGroupService,
        AuditService auditService) : Controller
    {
        private readonly TestService _testService = testService;
        private readonly TestGroupService _testGroupService = testGroupService;
        private readonly AuditService _auditService = auditService;

        public async Task<IActionResult> Index()
        {
            var tests = await _testService.GetAllAsync<TestViewModel>(i=>i.Include(t => t.TestGroup));

            LogHelper.LogWithContext("Viewed tests list", User?.Identity?.Name ?? "Unknown", "Admin", "TestsController.Index", LogEventLevel.Information);

            return View(tests);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new TestViewModel();
            ViewBag.AvailableGroups = await _testGroupService.GetAllAsync<TestGroupViewModel>();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AvailableGroups = await _testGroupService.GetAllAsync<TestGroupViewModel>();
                return View(model);
            }

            var createdTest = await _testService.AddAsync(model);

            await _auditService.WriteAsync(
                EnAuditAction.EntityCreated,
                $"Admin created test '{createdTest.Name}'",
                actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                actorUserName: User.Identity?.Name,
                entityName: nameof(Test),
                entityId: createdTest.Id.ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            LogHelper.LogWithContext($"Created test {model.Name}", User?.Identity?.Name ?? "Unknown", "Admin", "TestsController.Create", LogEventLevel.Warning);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var test = await _testService.GetByIdAsync(id);
            if (test == null) return NotFound();

            var model = new TestViewModel
            {
                Id = test.Id,
                Name = test.Name,
                TestGroupId = test.TestGroupId,
            };
            ViewBag.AvailableGroups = await _testGroupService.GetAllAsync<TestGroupViewModel>();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AvailableGroups = await _testGroupService.GetAllAsync<TestGroupViewModel>();
                return View(model);
            }

            var updatedTest = await _testService.UpdateAsync(model.Id, model);
            if (updatedTest == null)
                return NotFound();

            await _auditService.WriteAsync(
                EnAuditAction.EntityUpdated,
                $"Admin updated test '{updatedTest.Name}'",
                actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                actorUserName: User.Identity?.Name,
                entityName: nameof(Test),
                entityId: model.Id.ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            LogHelper.LogWithContext($"Edited test {model.Name}", User?.Identity?.Name ?? "Unknown", "Admin", "TestsController.Edit", LogEventLevel.Warning);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var test = await _testService.GetByIdAsync(id);
            if (test == null)
            {
                LogHelper.LogWithContext($"Attempted to delete non-existing test with ID {id}", User?.Identity?.Name ?? "Unknown", "Admin", "TestsController.Delete", LogEventLevel.Error);
                return NotFound();
            }

            await _testService.DeleteAsync(id);

            await _auditService.WriteAsync(
                EnAuditAction.EntityDeleted,
                $"Admin deleted test '{test.Name}'",
                actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                actorUserName: User.Identity?.Name,
                entityName: nameof(Test),
                entityId: id.ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            LogHelper.LogWithContext($"Deleted test {test.Name}", User?.Identity?.Name ?? "Unknown", "Admin", "TestsController.Delete", LogEventLevel.Warning);

            return RedirectToAction("Index");
        }
    }
}
