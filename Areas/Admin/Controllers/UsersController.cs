using AutoMapper;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;
using IPTS.Helpers;
using IPTS.Resources;

namespace IPTS.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = "admin")]
    public class UsersController(
        EmailService emailService,
        UserService userService,
        IMapper mapper,
        RoleManager<IdentityRole> roleManager,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        SpecialtyService specialtyService,
        LocService locService,
        AuditService auditService
        ) : Controller
    {
        private readonly LocService _locService = locService;
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly SignInManager<AppUser> _signInManager = signInManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly EmailService _emailService = emailService;
        private readonly IMapper _mapper = mapper;
        private readonly UserService _userService = userService;
        private readonly SpecialtyService _specialtyService = specialtyService;
        private readonly AuditService _auditService = auditService;

        public async Task<IActionResult> Index(string status = "active")
        {
            try
            {
                var showInactive = string.Equals(status, "inactive", StringComparison.OrdinalIgnoreCase);
                ViewBag.StatusFilter = showInactive ? "inactive" : "active";

                var users = await _userService
                    .GetAllAsync<UserListViewModel>(u => u
                        .Include(u => u.UserType)
                        .Where(u => showInactive
                            ? u.Status == EnUserStatus.Deleted
                            : u.Status != EnUserStatus.Deleted))
                    ?? new List<UserListViewModel>();

                LogHelper.LogWithContext(
                    $"Viewed {(showInactive ? "inactive" : "active")} users list",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.Index",
                    LogEventLevel.Warning
                );

                return View(users);
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error loading users list: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.Index",
                    LogEventLevel.Fatal
                );
                throw;
            }
        }

        [HttpGet("UserForm/{UserType}/{id?}")]
        public async Task<IActionResult> UserForm(string UserType, string? id)
        {
            ViewBag.UserType = UserType;
            var model = new UserFormViewModel();

            try
            {
                if (!string.IsNullOrEmpty(id)) // Edit mode
                {
                    var user = await _userService.GetByIdAsync(id, i => i.Include(u => u.Admin).Include(u => u.Doctor).Include(u => u.Patient).Include(u=> u.UserType));
                    if (user == null) return NotFound();

                   
                    model = _mapper.Map<UserFormViewModel>(user);

                    LogHelper.LogWithContext(
                        $"Opened edit form for user {id}",
                        User?.Identity?.Name ?? "Unknown",
                        "Admin",
                        "UsersController.UserForm",
                        LogEventLevel.Warning
                    );
                }
                else
                {
                    LogHelper.LogWithContext(
                        $"Opened create user form",
                        User?.Identity?.Name ?? "Unknown",
                        "Admin",
                        "UsersController.UserForm",
                        LogEventLevel.Warning
                    );
                }
                await PrepareUserFormViewBagAsync(UserType);

                return View(model);
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error opening user form: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.UserForm",
                    LogEventLevel.Fatal
                );
                throw;
            }
        }

        [HttpPost("UserForm/{UserType}")]
        public async Task<IActionResult> UserFormAsync([FromForm] UserFormViewModel model, string UserType)
        {
            ViewBag.UserType = UserType;

            if (!ModelState.IsValid)
            {
                LogHelper.LogWithContext(
                    "Invalid user form submission",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.UserFormAsync",
                    LogEventLevel.Warning
                );

                await PrepareUserFormViewBagAsync(UserType);
                return View("UserForm", model);
            }

            try
            {
                if (string.IsNullOrEmpty(model.Id))
                {
                    await _userService.CreateAsync(model, UserType);
                    var createdUser = string.IsNullOrWhiteSpace(model.UserName)
                        ? null
                        : await _userManager.FindByNameAsync(model.UserName);

                    await _auditService.WriteAsync(
                        EnAuditAction.UserCreated,
                        $"Admin created {UserType} user '{model.UserName}'",
                        actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                        actorUserName: User.Identity?.Name,
                        targetUserId: createdUser?.Id,
                        entityName: nameof(AppUser),
                        entityId: createdUser?.Id,
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

                    LogHelper.LogWithContext(
                        $"Created new user {model.UserName}",
                        User?.Identity?.Name ?? "Unknown",
                        "Admin",
                        "UsersController.UserFormAsync",
                        LogEventLevel.Warning
                    );
                    TempData["SuccessMessage"] = _locService.GetSystem("Msg_CreateSuccess");
                }
                else
                {
                    await _userService.UpdateAsync(model, UserType);

                    await _auditService.WriteAsync(
                        EnAuditAction.UserUpdated,
                        $"Admin updated {UserType} user '{model.UserName}'",
                        actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                        actorUserName: User.Identity?.Name,
                        targetUserId: model.Id,
                        entityName: nameof(AppUser),
                        entityId: model.Id,
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

                    LogHelper.LogWithContext(
                        $"Updated user {model.UserName} (Id: {model.Id})",
                        User?.Identity?.Name ?? "Unknown",
                        "Admin",
                        "UsersController.UserFormAsync",
                        LogEventLevel.Warning
                    );
                    TempData["SuccessMessage"] = _locService.GetSystem("Msg_UpdateSuccess");
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error processing user form: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.UserFormAsync",
                    LogEventLevel.Fatal
                );
                ModelState.AddModelError(string.Empty, _locService.GetSystem("Msg_ErrorSave"));
                await PrepareUserFormViewBagAsync(UserType);
                return View("UserForm", model);
            }
        }

        private async Task PrepareUserFormViewBagAsync(string? userType)
        {
            if (string.Equals(userType, "doctor", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Specialties = await _specialtyService.GetAllAsync();
            }
        }

        public async Task<IActionResult> SendRestPasswordLink(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                LogHelper.LogWithContext(
                    "Invalid password reset form submission",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.SendRestPasswordLink",
                    LogEventLevel.Warning
                );
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    LogHelper.LogWithContext(
                        $"Password reset requested for non-existing email: {model.Email}",
                        User?.Identity?.Name ?? "Unknown",
                        "Admin",
                        "UsersController.SendRestPasswordLink",
                        LogEventLevel.Warning
                    );
                   TempData["ErrorMessage"] = _locService.GetSystem("Msg_ErrorResetLink");
                    return View(model);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action("ResetPasswordConfirm", "Auth", new { token, email = model.Email }, Request.Scheme);

                await _emailService.SendEmail(model.Email, _locService.GetSystem("Email_ResetTitle"),
                    $"<p>{_locService.GetSystem("Email_Hello")},</p><p>{_locService.GetSystem("Email_ResetRequest")}.</p>" +
                    $"<p>{_locService.GetSystem("Email_ResetInstruction")}</p>" +
                    $"<p><a href='{resetLink}'>{_locService.GetSystem("Email_ResetButton")}</a></p>" +
                    $"<p>{_locService.GetSystem("Email_IgnoreRequest")}</p>"
                );

                await _auditService.WriteAsync(
                    EnAuditAction.PasswordResetRequested,
                    $"Admin sent a password reset link to user '{user.UserName}'",
                    actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    actorUserName: User.Identity?.Name,
                    targetUserId: user.Id,
                    entityName: nameof(AppUser),
                    entityId: user.Id,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

                LogHelper.LogWithContext(
                    $"Password reset link sent to {model.Email}",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.SendRestPasswordLink",
                    LogEventLevel.Warning
                );

                TempData["SuccessMessage"] = _locService.GetSystem("Msg_LinkSentSuccess");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error sending password reset link: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.SendRestPasswordLink",
                    LogEventLevel.Fatal
                );
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                LogHelper.LogWithContext(
                    "Delete called with empty Id",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.Delete",
                    LogEventLevel.Warning
                );
                return BadRequest();
            }

            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                {
                    LogHelper.LogWithContext(
                        $"Delete requested for non-existing user Id: {id}",
                        User?.Identity?.Name ?? "Unknown",
                        "Admin",
                        "UsersController.Delete",
                        LogEventLevel.Warning
                    );
                    return NotFound();
                }

                user.Status = EnUserStatus.Deleted;
                await _userManager.UpdateAsync(user);

                await _auditService.WriteAsync(
                    EnAuditAction.UserDeleted,
                    $"Admin marked user '{user.UserName}' as deleted",
                    actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    actorUserName: User.Identity?.Name,
                    targetUserId: user.Id,
                    entityName: nameof(AppUser),
                    entityId: user.Id,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

                LogHelper.LogWithContext(
                    $"User {id} marked as deleted",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.Delete",
                    LogEventLevel.Warning
                );

                TempData["SuccessMessage"] = _locService.GetSystem("Msg_DeleteSuccess");
                return RedirectToAction(nameof(Index), new { status = "active" });
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error deleting user {id}: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.Delete",
                    LogEventLevel.Fatal
                );
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Reactivate(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                LogHelper.LogWithContext(
                    "Reactivate called with empty Id",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.Reactivate",
                    LogEventLevel.Warning
                );
                return BadRequest();
            }

            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                {
                    LogHelper.LogWithContext(
                        $"Reactivate requested for non-existing user Id: {id}",
                        User?.Identity?.Name ?? "Unknown",
                        "Admin",
                        "UsersController.Reactivate",
                        LogEventLevel.Warning
                    );
                    return NotFound();
                }

                user.Status = EnUserStatus.Active;
                await _userManager.UpdateAsync(user);

                await _auditService.WriteAsync(
                    EnAuditAction.UserUpdated,
                    $"Admin reactivated user '{user.UserName}'",
                    actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    actorUserName: User.Identity?.Name,
                    targetUserId: user.Id,
                    entityName: nameof(AppUser),
                    entityId: user.Id,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

                LogHelper.LogWithContext(
                    $"User {id} reactivated",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.Reactivate",
                    LogEventLevel.Warning
                );

                TempData["SuccessMessage"] = _locService.GetSystem("Msg_ReactivateSuccess");
                return RedirectToAction(nameof(Index), new { status = "inactive" });
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error reactivating user {id}: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.Reactivate",
                    LogEventLevel.Fatal
                );
                throw;
            }
        }
    }
}
