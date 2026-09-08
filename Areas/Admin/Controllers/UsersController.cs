using AutoMapper;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Data;
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
        AuditService auditService,
        ApplicationDbContext context
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
        private readonly ApplicationDbContext _context = context;

        public async Task<IActionResult> Index(string status = "active", int page = 1, int pageSize = 10)
        {
            try
            {
                var showInactive = string.Equals(status, "inactive", StringComparison.OrdinalIgnoreCase);
                var (users, totalCount) = await _userService.GetPagedUsersAsync(showInactive, page, pageSize);

                var model = new UserListPageViewModel
                {
                    Items = users,
                    Page = page < 1 ? 1 : page,
                    PageSize = pageSize < 1 ? 10 : pageSize,
                    TotalCount = totalCount,
                    StatusFilter = showInactive ? "inactive" : "active"
                };

                LogHelper.LogWithContext(
                    $"Viewed {(showInactive ? "inactive" : "active")} users list. page={model.Page}, total={totalCount}",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.Index",
                    LogEventLevel.Information
                );

                return View(model);
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

                if (string.Equals(UserType, "patient", StringComparison.OrdinalIgnoreCase))
                {
                    model.Patient ??= new PatientFormViewModel();
                    if (string.IsNullOrEmpty(id) && model.Patient.BirthDate == default)
                        model.Patient.BirthDate = DateTime.Today;
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
                if (string.Equals(UserType, "patient", StringComparison.OrdinalIgnoreCase))
                    model.Patient ??= new PatientFormViewModel();
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
                if (string.Equals(UserType, "patient", StringComparison.OrdinalIgnoreCase))
                    model.Patient ??= new PatientFormViewModel();
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

        [HttpGet]
        public async Task<IActionResult> AssignDoctor(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest();

            try
            {
                var user = await _userService.GetByIdAsync(id, q => q.Include(u => u.Patient).Include(u => u.UserType));
                if (user?.Patient == null || !string.Equals(user.UserType?.Name, "patient", StringComparison.OrdinalIgnoreCase))
                    return NotFound();

                await LoadDoctorsAsync();

                LogHelper.LogWithContext(
                    $"Opened assign-doctor form for patient {id}",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.AssignDoctor",
                    LogEventLevel.Information);

                return View(new AssignDoctorViewModel
                {
                    PatientId = user.Patient.Id,
                    UserId = user.Id,
                    PatientName = $"{user.FirstName} {user.LastName}".Trim(),
                    PatientEmail = user.Email ?? string.Empty,
                    AssignedDoctorId = user.Patient.AssignedDoctorId ?? 0
                });
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error opening assign-doctor form: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.AssignDoctor",
                    LogEventLevel.Error);
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDoctor(AssignDoctorViewModel model)
        {
            await LoadDoctorsAsync();

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == model.PatientId);
                if (patient == null)
                    return NotFound();

                var doctorExists = await _context.Doctors.AnyAsync(d => d.Id == model.AssignedDoctorId);
                if (!doctorExists)
                {
                    ModelState.AddModelError(nameof(model.AssignedDoctorId), _locService.GetSystem("AssignDoctor_DoctorRequired"));
                    return View(model);
                }

                patient.AssignedDoctorId = model.AssignedDoctorId;
                await _context.SaveChangesAsync();

                await _auditService.WriteAsync(
                    EnAuditAction.EntityUpdated,
                    $"Admin assigned patient '{patient.User?.UserName}' to doctor {model.AssignedDoctorId}",
                    actorUserId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    actorUserName: User.Identity?.Name,
                    targetUserId: patient.UserId,
                    entityName: nameof(Patient),
                    entityId: patient.Id.ToString(),
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

                LogHelper.LogWithContext(
                    $"Assigned patient {patient.Id} to doctor {model.AssignedDoctorId}",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.AssignDoctor",
                    LogEventLevel.Warning);

                TempData["SuccessMessage"] = _locService.GetSystem("AssignDoctor_Success");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error assigning doctor: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.AssignDoctor",
                    LogEventLevel.Fatal);
                ModelState.AddModelError(string.Empty, _locService.GetSystem("Msg_ErrorSave"));
                return View(model);
            }
        }

        private async Task LoadDoctorsAsync()
        {
            ViewBag.Doctors = await _context.Doctors
                .AsNoTracking()
                .Include(d => d.User)
                .Where(d => d.User != null && d.User.Status != EnUserStatus.Deleted)
                .OrderBy(d => d.User!.LastName)
                .ThenBy(d => d.User!.FirstName)
                .ToListAsync();
        }
    }
}
