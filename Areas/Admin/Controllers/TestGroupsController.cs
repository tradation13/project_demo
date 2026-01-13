using IPTS.Areas.Admin.ViewsModels;
using IPTS.Models.Entites;
using IPTS.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;
using IPTS.Data;
using IPTS.Services;

namespace IPTS.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = "admin")]
    public class TestGroupsController(TestGroupService testGroupService) : Controller
    {
        private readonly TestGroupService _testGroupService = testGroupService;

        public async Task<IActionResult> Index()
        {
            var groups = await testGroupService.GetAllAsync<TestGroupViewModel>();

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

            await testGroupService.AddAsync(model);

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

            if(model.Id != null)
            {
                await _testGroupService.UpdateAsync((int)model.Id, model);
            }

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

            LogHelper.LogWithContext($"Deleted test group {group.Name}", User?.Identity?.Name ?? "Unknown", "Admin", "TestGroupsController.Delete", LogEventLevel.Warning);

            return RedirectToAction("Index");
        }
    }
}
