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
        SpecialtyService specialtyService
        ) : Controller
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly SignInManager<AppUser> _signInManager = signInManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly EmailService _emailService = emailService;
        private readonly IMapper _mapper = mapper;
        private readonly UserService _userService = userService;
        private readonly SpecialtyService _specialtyService = specialtyService;

        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userService
                    .GetAllAsync<UserListViewModel>(u => u.Include(u => u.UserType).Where(u => u.Status != EnUserStatus.Deleted))
                    ?? new List<UserListViewModel>();

                // Log important action
                LogHelper.LogWithContext(
                    "Viewed users list",
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
                if (UserType?.ToLower() == "doctor")
                {
                    ViewBag.Specialties = await _specialtyService.GetAllAsync();
                }

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
                return View(model);
            }

            try
            {
                if (string.IsNullOrEmpty(model.Id))
                {
                    await _userService.CreateAsync(model, UserType);
                    LogHelper.LogWithContext(
                        $"Created new user {model.UserName}",
                        User?.Identity?.Name ?? "Unknown",
                        "Admin",
                        "UsersController.UserFormAsync",
                        LogEventLevel.Warning
                    );
                    TempData["SuccessMessage"] = "User created successfully.";
                }
                else
                {
                    await _userService.UpdateAsync(model, UserType);
                    LogHelper.LogWithContext(
                        $"Updated user {model.UserName} (Id: {model.Id})",
                        User?.Identity?.Name ?? "Unknown",
                        "Admin",
                        "UsersController.UserFormAsync",
                        LogEventLevel.Warning
                    );
                    TempData["SuccessMessage"] = "User updated successfully.";
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
                throw;
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
                    TempData["ErrorMessage"] = "Cannot send reset link for this email.";
                    return View(model);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action("ResetPasswordConfirm", "Auth", new { token, email = model.Email }, Request.Scheme);

                await _emailService.SendEmail(model.Email, "Reset your password",
                    $"<p>Hello,</p><p>You requested to reset your password.</p>" +
                    $"<p>Please click the link below to set a new password:</p>" +
                    $"<p><a href='{resetLink}'>Reset Password</a></p>" +
                    "<p>If you did not request this, please ignore this email.</p>"
                );

                LogHelper.LogWithContext(
                    $"Password reset link sent to {model.Email}",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.SendRestPasswordLink",
                    LogEventLevel.Warning
                );

                TempData["SuccessMessage"] = "Password Reset link has been sent to the user email.";
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

                LogHelper.LogWithContext(
                    $"User {id} marked as deleted",
                    User?.Identity?.Name ?? "Unknown",
                    "Admin",
                    "UsersController.Delete",
                    LogEventLevel.Warning
                );

                TempData["SuccessMessage"] = "User has been deleted successfully.";
                return RedirectToAction("Index");
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
    }
}
