using IPTS.Models.Entites;
using IPTS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

[Area("admin")]
[Authorize(Roles = "admin")]
public class UsersTypesController(UserTypeService userTypeService, RoleManager<IdentityRole> roleManager) : Controller
{
    private readonly UserTypeService _userTypeService = userTypeService;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;

    public async Task<IActionResult> Index()
    {
        var userTypes = await _userTypeService.GetAllAsync() ?? new List<UserType>();
        return View(userTypes);
    }

    public async Task<IActionResult> Create()
    {
        var model = new UserType();

        ViewData["Roles"] = new SelectList(_roleManager.Roles, "Id", "Name");

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(UserType model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Roles"] = new SelectList(_roleManager.Roles, "Id", "Name", model.DefaultRoleId);
            return View(model);
        }

        await _userTypeService.AddAsync(model);
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var model = await _userTypeService.GetByIdAsync<int, UserType>(id);
        if (model == null)
            return NotFound();

        ViewData["Roles"] = new SelectList(_roleManager.Roles, "Id", "Name", model.DefaultRoleId);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, UserType model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Roles"] = new SelectList(_roleManager.Roles, "Id", "Name", model.DefaultRoleId);
            return View(model);
        }

        var updated = await _userTypeService.UpdateAsync(id, model);
        if (updated == null)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userTypeService.DeleteAsync(id);
        if (!result)
            return NotFound();

        return RedirectToAction(nameof(Index));
    }
}
