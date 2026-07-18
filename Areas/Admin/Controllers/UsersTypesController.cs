using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

[Area("admin")]
[Authorize(Roles = "admin")]
public class UsersTypesController(
    UserTypeService userTypeService,
    RoleManager<IdentityRole> roleManager,
    AuditService auditService) : Controller
{
    private readonly UserTypeService _userTypeService = userTypeService;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly AuditService _auditService = auditService;

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

        var createdUserType = await _userTypeService.AddAsync(model);

        await _auditService.WriteAsync(
            EnAuditAction.EntityCreated,
            $"Admin created user type '{createdUserType.Name}'",
            actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            actorUserName: User.Identity?.Name,
            entityName: nameof(UserType),
            entityId: createdUserType.Id.ToString(),
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

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

        await _auditService.WriteAsync(
            EnAuditAction.EntityUpdated,
            $"Admin updated user type '{updated.Name}'",
            actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            actorUserName: User.Identity?.Name,
            entityName: nameof(UserType),
            entityId: id.ToString(),
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var userType = await _userTypeService.GetByIdAsync(id);
        if (userType == null)
            return NotFound();

        var result = await _userTypeService.DeleteAsync(id);
        if (!result)
            return NotFound();

        await _auditService.WriteAsync(
            EnAuditAction.EntityDeleted,
            $"Admin deleted user type '{userType.Name}'",
            actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            actorUserName: User.Identity?.Name,
            entityName: nameof(UserType),
            entityId: id.ToString(),
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

        return RedirectToAction(nameof(Index));
    }
}
