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
        TestParameterService testParameterService,
        AuditService auditService) : Controller
    {
        private readonly TestService _testService = testService;
        private readonly TestGroupService _testGroupService = testGroupService;
        private readonly TestParameterService _testParameterService = testParameterService;
        private readonly AuditService _auditService = auditService;

        public async Task<IActionResult> Index()
        {
            var tests = await _testService.GetAllAsync<TestViewModel>(i => i.Include(t => t.TestGroup));

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
                StandardValue = test.StandardValue,
            };
            await FillEditBags(id);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await FillEditBags(model.Id);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddParameter(TestParameterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = string.Join(" ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return RedirectToAction("Edit", new { id = model.TestId });
            }

            if (model.Date.HasValue)
                model.Date = DateTime.SpecifyKind(model.Date.Value.Date, DateTimeKind.Utc);

            await _testParameterService.AddAsync(model);

            await _auditService.WriteAsync(
                EnAuditAction.EntityCreated,
                $"Admin added parameter '{model.Key}' to test {model.TestId}",
                actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                actorUserName: User.Identity?.Name,
                entityName: nameof(TestParameter),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            LogHelper.LogWithContext($"Added parameter {model.Key} to test {model.TestId}", User?.Identity?.Name ?? "Unknown", "Admin", "TestsController.AddParameter", LogEventLevel.Information);

            return RedirectToAction("Edit", new { id = model.TestId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteParameter(int id, int testId)
        {
            var parameter = await _testParameterService.GetByIdAsync(id);
            if (parameter == null || parameter.TestId != testId)
                return NotFound();

            await _testParameterService.DeleteAsync(id);

            await _auditService.WriteAsync(
                EnAuditAction.EntityDeleted,
                $"Admin deleted parameter '{parameter.Key}' from test {testId}",
                actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                actorUserName: User.Identity?.Name,
                entityName: nameof(TestParameter),
                entityId: id.ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            LogHelper.LogWithContext($"Deleted parameter {parameter.Key} from test {testId}", User?.Identity?.Name ?? "Unknown", "Admin", "TestsController.DeleteParameter", LogEventLevel.Warning);

            return RedirectToAction("Edit", new { id = testId });
        }

        private async Task FillEditBags(int testId)
        {
            ViewBag.AvailableGroups = await _testGroupService.GetAllAsync<TestGroupViewModel>();
            ViewBag.Parameters = await _testParameterService.GetAllAsync<TestParameterViewModel>(
                q => q.Where(p => p.TestId == testId).OrderBy(p => p.Key));
        }
    }
}
