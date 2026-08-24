using IPTS.Data;
using IPTS.Helpers;
using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.Resources;
using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IPTS.Controllers
{
    public class AuthController(
        LocService locService,
        EmailService emailService,
        ILogger<AuthController> logger,
        UserManager<AppUser> userManager,
        UserService userService,
        SignInManager<AppUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IdentityErrorTranslator identityErrorTranslator,
        AuditService auditService) : Controller
    {

        private readonly LocService _locService = locService;
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly SignInManager<AppUser> _signInManager = signInManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly UserService _userService = userService;
        private readonly EmailService _emailService = emailService;
        private readonly ILogger<AuthController> _logger = logger;
        private readonly ApplicationDbContext _context = context;
        private readonly IdentityErrorTranslator _identityErrorTranslator = identityErrorTranslator;
        private readonly AuditService _auditService = auditService;

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return View(new LoginViewModel { ReturnUrl = returnUrl });

                var redirectedRoute = GetRedirectedRoute(currentUser);

                return RedirectToAction(
                    actionName: redirectedRoute.ActionName,
                    controllerName: redirectedRoute.Controller,
                    routeValues: new { area = redirectedRoute.Area });
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var returnUrl = model.ReturnUrl;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (!ModelState.IsValid)
                return View(model);

            AppUser? user = model.UsernameOrEmail.Contains('@')
                ? await _userManager.FindByEmailAsync(model.UsernameOrEmail)
                : await _userManager.FindByNameAsync(model.UsernameOrEmail);

            if (user == null)
            {
                LogHelper.LogWithContext(
                    $"Failed login for unknown identity '{model.UsernameOrEmail}'",
                    "Anonymous",
                    "Guest",
                    "Auth.Login",
                    LogEventLevel.Warning);

                await _auditService.WriteAsync(
                    EnAuditAction.LoginFailed,
                    $"Failed login for unknown identity '{model.UsernameOrEmail}'",
                    actorUserName: model.UsernameOrEmail,
                    ipAddress: ip);

                ModelState.AddModelError(string.Empty, _locService.GetSystem("Auth_InvalidLogin"));
                return View(model);
            }

            if (user.Status != EnUserStatus.Active)
            {
                await _auditService.WriteAsync(
                    EnAuditAction.LoginFailed,
                    "Login blocked because user is inactive",
                    actorUserId: user.Id,
                    actorUserName: user.UserName,
                    targetUserId: user.Id,
                    ipAddress: ip);

                ModelState.AddModelError(string.Empty, _locService.GetSystem("Auth_UserInactive"));
                TempData["ErrorMessage"] = _locService.GetSystem("Auth_UserInactive");
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                TempData["WarningMessage"] = _locService.GetSystem("Auth_EmailNotVerified");
                return RedirectToAction("VerifyEmail", new { email = user.Email});
            }

            var signInResult = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (!signInResult.Succeeded)
            {
                if (signInResult.IsLockedOut)
                {
                    LogHelper.LogWithContext(
                        $"Account locked for user '{user.UserName}'",
                        user.Id,
                        "Auth",
                        "Auth.Login",
                        LogEventLevel.Warning);

                    await _auditService.WriteAsync(
                        EnAuditAction.AccountLocked,
                        "Account temporarily locked after failed login attempts",
                        actorUserId: user.Id,
                        actorUserName: user.UserName,
                        targetUserId: user.Id,
                        ipAddress: ip);

                    ModelState.AddModelError(string.Empty, _locService.GetSystem("Auth_LockoutTemporary"));
                    return View(model);
                }

                LogHelper.LogWithContext(
                    $"Failed login for user '{user.UserName}'",
                    user.Id,
                    "Auth",
                    "Auth.Login",
                    LogEventLevel.Warning);

                await _auditService.WriteAsync(
                    EnAuditAction.LoginFailed,
                    "Invalid password",
                    actorUserId: user.Id,
                    actorUserName: user.UserName,
                    targetUserId: user.Id,
                    ipAddress: ip);

                ModelState.AddModelError(string.Empty, _locService.GetSystem("Auth_InvalidLogin"));
                return View(model);
            }

            LogHelper.LogWithContext(
                $"Successful login for user '{user.UserName}'",
                user.Id,
                "Auth",
                "Auth.Login",
                LogEventLevel.Information);

            await _auditService.WriteAsync(
                EnAuditAction.LoginSuccess,
                "User signed in successfully",
                actorUserId: user.Id,
                actorUserName: user.UserName,
                targetUserId: user.Id,
                ipAddress: ip);

            var redirectedRoute = GetRedirectedRoute(user);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(
                            actionName: redirectedRoute.ActionName,
                            controllerName: redirectedRoute.Controller,
                            routeValues: new { area = redirectedRoute.Area });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> ApiLogin([FromBody] GuestLoginRequest model)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (!ModelState.IsValid)
            {
                return StatusCode(StatusCodes.Status400BadRequest, ApiResult<GuestAuthSuccessDto>.Fail(
                    "VALIDATION_ERROR",
                    _locService.GetSystem("Auth_InvalidLogin"),
                    ApiRequestHelper.ToErrorDictionary(ModelState)));
            }

            AppUser? user = model.UsernameOrEmail.Contains('@')
                ? await _userManager.FindByEmailAsync(model.UsernameOrEmail)
                : await _userManager.FindByNameAsync(model.UsernameOrEmail);

            if (user == null)
            {
                LogHelper.LogWithContext(
                    $"Failed modal login for unknown identity '{model.UsernameOrEmail}'",
                    "Anonymous",
                    "Guest",
                    "Auth.ApiLogin",
                    LogEventLevel.Warning);

                await _auditService.WriteAsync(
                    EnAuditAction.LoginFailed,
                    $"Failed modal login for unknown identity '{model.UsernameOrEmail}'",
                    actorUserName: model.UsernameOrEmail,
                    ipAddress: ip);

                return StatusCode(StatusCodes.Status401Unauthorized, ApiResult<GuestAuthSuccessDto>.Fail(
                    "INVALID_CREDENTIALS",
                    _locService.GetSystem("Auth_InvalidLogin")));
            }

            if (user.Status != EnUserStatus.Active)
            {
                await _auditService.WriteAsync(
                    EnAuditAction.LoginFailed,
                    "Modal login blocked because user is inactive",
                    actorUserId: user.Id,
                    actorUserName: user.UserName,
                    targetUserId: user.Id,
                    ipAddress: ip);

                return StatusCode(StatusCodes.Status403Forbidden, ApiResult<GuestAuthSuccessDto>.Fail(
                    "USER_INACTIVE",
                    _locService.GetSystem("Auth_UserInactive")));
            }

            if (!user.EmailConfirmed)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResult<GuestAuthSuccessDto>.Fail(
                    "EMAIL_NOT_CONFIRMED",
                    _locService.GetSystem("Auth_EmailNotVerified"),
                    data: new GuestAuthSuccessDto
                    {
                        IsEmailConfirmed = false,
                        Email = user.Email,
                        Role = await GetPrimaryRoleAsync(user)
                    }));
            }

            var signInResult = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (!signInResult.Succeeded)
            {
                if (signInResult.IsLockedOut)
                {
                    LogHelper.LogWithContext(
                        $"Account locked for user '{user.UserName}' during modal login",
                        user.Id,
                        "Auth",
                        "Auth.ApiLogin",
                        LogEventLevel.Warning);

                    await _auditService.WriteAsync(
                        EnAuditAction.AccountLocked,
                        "Account temporarily locked after failed modal login attempts",
                        actorUserId: user.Id,
                        actorUserName: user.UserName,
                        targetUserId: user.Id,
                        ipAddress: ip);

                    return StatusCode(StatusCodes.Status423Locked, ApiResult<GuestAuthSuccessDto>.Fail(
                        "ACCOUNT_LOCKED",
                        _locService.GetSystem("Auth_LockoutTemporary")));
                }

                LogHelper.LogWithContext(
                    $"Failed modal login for user '{user.UserName}'",
                    user.Id,
                    "Auth",
                    "Auth.ApiLogin",
                    LogEventLevel.Warning);

                await _auditService.WriteAsync(
                    EnAuditAction.LoginFailed,
                    "Invalid password (modal)",
                    actorUserId: user.Id,
                    actorUserName: user.UserName,
                    targetUserId: user.Id,
                    ipAddress: ip);

                return StatusCode(StatusCodes.Status401Unauthorized, ApiResult<GuestAuthSuccessDto>.Fail(
                    "INVALID_CREDENTIALS",
                    _locService.GetSystem("Auth_InvalidLogin")));
            }

            var role = await GetPrimaryRoleAsync(user);
            if (!string.Equals(role, "patient", StringComparison.OrdinalIgnoreCase))
            {
                await _signInManager.SignOutAsync();
                return StatusCode(StatusCodes.Status403Forbidden, ApiResult<GuestAuthSuccessDto>.Fail(
                    "FORBIDDEN_ROLE",
                    _locService.GetSystem("Auth_BookingPatientOnly")));
            }

            LogHelper.LogWithContext(
                $"Successful modal login for user '{user.UserName}'",
                user.Id,
                "Auth",
                "Auth.ApiLogin",
                LogEventLevel.Information);

            await _auditService.WriteAsync(
                EnAuditAction.LoginSuccess,
                "User signed in from booking modal",
                actorUserId: user.Id,
                actorUserName: user.UserName,
                targetUserId: user.Id,
                ipAddress: ip);

            return Ok(ApiResult<GuestAuthSuccessDto>.Success(
                "LOGIN_SUCCESS",
                _locService.GetSystem("Auth_ModalLoginSuccess"),
                new GuestAuthSuccessDto
                {
                    IsEmailConfirmed = true,
                    Role = role,
                    Email = user.Email
                }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> ApiRegister([FromBody] GuestRegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                return StatusCode(StatusCodes.Status400BadRequest, ApiResult<GuestAuthSuccessDto>.Fail(
                    "VALIDATION_ERROR",
                    _locService.GetSystem("Code_RegistrationFailed"),
                    ApiRequestHelper.ToErrorDictionary(ModelState)));
            }

            string? bookingDoctorUserId = null;
            if (!string.IsNullOrWhiteSpace(model.DoctorUserId))
            {
                var doctorUser = await _userManager.FindByIdAsync(model.DoctorUserId);
                if (doctorUser != null && await _userManager.IsInRoleAsync(doctorUser, "doctor"))
                    bookingDoctorUserId = doctorUser.Id;
            }

            var registerModel = new RegisterViewModel
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = model.Email.Trim(),
                UserName = model.Email.Trim(),
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                UserTypeName = "patient",
                AcceptPrivacy = model.AcceptPrivacy,
                AcceptTerms = model.AcceptTerms,
                AcceptHealthDataConsent = model.AcceptHealthDataConsent,
                Patient = new PatientRegisterViewModel()
            };

            var result = await _userService.RegisterAsync(registerModel, bookingDoctorUserId);
            if (!result.Succeeded)
            {
                var translatedErrors = _identityErrorTranslator.TranslateErrorsList(result.Errors);
                LogHelper.LogWithContext(
                    $"Modal registration failed for '{model.Email}': {string.Join(" | ", translatedErrors)}",
                    "Anonymous",
                    "Guest",
                    "Auth.ApiRegister",
                    LogEventLevel.Warning);

                var isConflict = translatedErrors.Any(e =>
                    e.Contains(_locService.GetSystem("Error_EmailAlreadyInUse"), StringComparison.OrdinalIgnoreCase)
                    || e.Contains(_locService.GetSystem("Error_UsernameTaken"), StringComparison.OrdinalIgnoreCase));

                return StatusCode(
                    isConflict ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest,
                    ApiResult<GuestAuthSuccessDto>.Fail(
                        isConflict ? "EMAIL_ALREADY_IN_USE" : "REGISTRATION_FAILED",
                        translatedErrors.FirstOrDefault() ?? _locService.GetSystem("Code_RegistrationFailed")));
            }

            LogHelper.LogWithContext(
                $"Modal registration succeeded for '{model.Email}'. Email confirmation required.",
                "Anonymous",
                "Guest",
                "Auth.ApiRegister",
                LogEventLevel.Information);

            return StatusCode(StatusCodes.Status201Created, ApiResult<GuestAuthSuccessDto>.Success(
                "EMAIL_CONFIRMATION_REQUIRED",
                _locService.GetSystem("Email_CheckInbox"),
                new GuestAuthSuccessDto
                {
                    IsEmailConfirmed = false,
                    Role = "patient",
                    Email = model.Email.Trim()
                }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> ApiResendConfirmation([FromBody] GuestResendConfirmationRequest model)
        {
            if (!ModelState.IsValid)
            {
                return StatusCode(StatusCodes.Status400BadRequest, ApiResult<object>.Fail(
                    "VALIDATION_ERROR",
                    _locService.GetSystem("EmailRequired"),
                    ApiRequestHelper.ToErrorDictionary(ModelState)));
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user != null && !user.EmailConfirmed)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(
                    "ConfirmEmail",
                    "Auth",
                    new { userId = user.Id, token },
                    Request.Scheme);

                await _emailService.SendEmail(
                    user.Email,
                    _locService.GetSystem("Email_VerifyTitle"),
                    $"{_locService.GetSystem("Email_VerifyInstruction")} <a href='{confirmationLink}'>{_locService.GetSystem("Email_VerifyButton")}</a>");

                LogHelper.LogWithContext(
                    $"Resent confirmation email to '{user.Email}'",
                    user.Id,
                    "Guest",
                    "Auth.ApiResendConfirmation");
            }

            return Ok(ApiResult<object>.Success(
                "CONFIRMATION_EMAIL_SENT",
                _locService.GetSystem("Auth_ResendSuccess")));
        }

        private async Task<string> GetPrimaryRoleAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault() ?? string.Empty;
        }
        private record RedirectRoute(string ActionName, string Controller, string Area);
        private RedirectRoute GetRedirectedRoute(AppUser user)
        {
            // Ensure UserType is loaded
            user.UserType ??= _userManager.Users
                .Where(u => u.Id == user.Id)
                .Select(u => u.UserType)
                .FirstOrDefault();

            var action = string.IsNullOrEmpty(user.UserType?.DefaultAction)
                        ? (user.UserType?.HasDashboard == true ? "Index" : "")
                        : user.UserType.DefaultController;
            var controller = string.IsNullOrEmpty(user.UserType?.DefaultController)
                ? (user.UserType?.HasDashboard == true ? "Dashboard" : "")
                : user.UserType.DefaultController;

            var area = string.IsNullOrEmpty(user.UserType?.DefaultArea)
                ? ""
                : user.UserType.DefaultArea;

            return new RedirectRoute(action, controller, area);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name;

            await _signInManager.SignOutAsync();

            await _auditService.WriteAsync(
                EnAuditAction.Logout,
                "User signed out",
                actorUserId: userId,
                actorUserName: userName,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            LogHelper.LogWithContext(
                $"User '{userName}' signed out",
                userId ?? "Unknown",
                "Auth",
                "Auth.Logout",
                LogEventLevel.Information);

            return RedirectToAction("Login", "Auth");
        }
        [HttpGet("ResetPassword")]
        [Authorize]
        public async Task<IActionResult> ResetPassword()
        {
            var currentUserId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var currentUser = await _userService.GetByIdAsync(currentUserId,
                u => u.Include(x => x.UserType));

            ViewBag.HasDashboard = currentUser.UserType?.HasDashboard ?? false;

            return View(new ResetPasswordViewModel());
        }
        [HttpPost("ResetPassword")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var currentUserId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            if (!ModelState.IsValid)
            {
                var currentUser = await _userService.GetByIdAsync(currentUserId,
                    u => u.Include(x => x.UserType));
                ViewBag.HasDashboard = currentUser.UserType?.HasDashboard ?? false;

                return View(model);
            }

            var result = await _userService.ChangePasswordAsync(currentUserId, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                var translatedErrors = _identityErrorTranslator.TranslateErrorsList(result.Errors);
                foreach (var error in translatedErrors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                var currentUser = await _userService.GetByIdAsync(currentUserId,
                    u => u.Include(x => x.UserType));
                ViewBag.HasDashboard = currentUser.UserType?.HasDashboard ?? false;
                TempData["ErrorMessage"] = string.Join(", ", translatedErrors);
                return View(model);
            }

            var user = await _userService.GetByIdAsync(currentUserId,
                u => u.Include(x => x.UserType));

            var redirectedRoute = GetRedirectedRoute(user);
            TempData["SuccessMessage"] = _locService.GetSystem("Auth_PasswordChanged");

            await _auditService.WriteAsync(
                EnAuditAction.PasswordChanged,
                "User changed their password",
                actorUserId: currentUserId,
                actorUserName: user.UserName,
                targetUserId: currentUserId,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            return RedirectToAction(
                actionName: redirectedRoute.ActionName,
                controllerName: redirectedRoute.Controller,
                routeValues: new { area = redirectedRoute.Area });
        }

        // ------------------- Action Come From Link From Login Page 

        [HttpGet("ForgotPassword")]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }
        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction("ForgotPasswordConfirmation");
            

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = Url.Action("ResetPasswordConfirm", "Auth", new { token, email = model.Email }, Request.Scheme);

            await _emailService.SendEmail(model.Email,
                _locService.GetSystem("Email_ResetTitle"),
                $"<p>{_locService.GetSystem("Email_Hello")}</p><p>{_locService.GetSystem("Email_ResetRequest")}</p>" +
                $"<p>{_locService.GetSystem("Email_ResetInstruction")}</p>" +
                $"<p><a href='{resetLink}'>{_locService.GetSystem("Email_ResetButton")}</a></p>" +
                $"<p>{_locService.GetSystem("Email_IgnoreRequest")}</p>");

            await _auditService.WriteAsync(
                EnAuditAction.PasswordResetRequested,
                $"Password reset requested for '{model.Email}'",
                actorUserId: user.Id,
                actorUserName: user.UserName,
                targetUserId: user.Id,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            return RedirectToAction("ForgotPasswordConfirmation");
        }
        [HttpGet("ForgotPasswordConfirmation")]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }


        // ---------------------- Action Come From Email 
        [HttpGet("ResetPasswordConfirm")]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirm(string token, string email)
        {
            if (token == null || email == null) return BadRequest();

            var model = new ResetPasswordConfirmViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost("ResetPasswordConfirm")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordConfirm(ResetPasswordConfirmViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null) return RedirectToAction("ResetPasswordConfirmation");
       
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded) return RedirectToAction("ResetPasswordConfirmation");
            
            var translatedErrors = _identityErrorTranslator.TranslateErrorsList(result.Errors);
            foreach (var error in translatedErrors) ModelState.AddModelError("", error);
            
            return View(model);
        }

        [HttpGet("ResetPasswordConfirmation")]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        // ---------------- Register Actions

        // Step 2: Show Registration Form
        [HttpGet("register")]
        public IActionResult Register(string? doctorUserId = null)
        {
            var model = new RegisterViewModel
            {
                Patient = new PatientRegisterViewModel(),
                BookingDoctorUserId = doctorUserId
            };

            return View(model);
        }
        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            model.Patient ??= new PatientRegisterViewModel();
            model.UserTypeName = "patient";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _userService.RegisterAsync(model, model.BookingDoctorUserId);

            if (!result.Succeeded)
            {
                var translatedErrors = _identityErrorTranslator.TranslateErrorsList(result.Errors);
                foreach (var error in translatedErrors)
                    ModelState.AddModelError(string.Empty, error);

                model.Patient ??= new PatientRegisterViewModel();
                return View(model);
            }

            TempData["SuccessMessage"] = _locService.GetSystem("Email_CheckInbox");
            return RedirectToAction("Login", "Auth");
        }
        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string? email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return View(); 
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, _locService.GetSystem("User_NotFound"));
                return View();
            }

            if (user.EmailConfirmed)
            {
                TempData["SuccessMessage"] = _locService.GetSystem("Auth_EmailAlreadyVerified");
                return RedirectToAction("Login");
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action(
                "ConfirmEmail",
                "Auth",
                new { userId = user.Id, token },
                Request.Scheme);

            await _emailService.SendEmail(
                user.Email,
                _locService.GetSystem("Email_VerifyTitle"),
                $"{_locService.GetSystem("Email_VerifyInstruction")} <a href='{confirmationLink}'>{_locService.GetSystem("Email_VerifyButton")}</a>"
            );
            TempData["SuccessMessage"] = _locService.GetSystem("Email_CheckInbox");
            return View(new VerifyEmailViewModel { Email = user.Email });
        }
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token, string? doctorUserId = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = _locService.GetSystem("Auth_VerifySuccess");
                await _signInManager.SignInAsync(user, false);

                if (!string.IsNullOrWhiteSpace(doctorUserId))
                {
                    var doctorUser = await _userManager.FindByIdAsync(doctorUserId);
                    if (doctorUser != null && await _userManager.IsInRoleAsync(doctorUser, "doctor"))
                    {
                        TempData["InfoMessage"] = _locService.GetSystem("Auth_EmailConfirmedSelectSlot");
                        LogHelper.LogWithContext(
                            $"Email confirmed; redirecting to booking page for doctorUserId={doctorUserId}",
                            user.Id,
                            "patient",
                            "Auth.ConfirmEmail");
                        return RedirectToAction("Index", "Appointment", new { area = "patient", Id = doctorUserId });
                    }
                }

                var redirectedRoute = GetRedirectedRoute(user);
                return RedirectToAction(
                    actionName: redirectedRoute.ActionName,
                    controllerName: redirectedRoute.Controller,
                    routeValues: new { area = redirectedRoute.Area });
            }

            TempData["ErrorMessage"] = _locService.GetSystem("Auth_VerifyFailed");
            return RedirectToAction("Login");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                TempData["ErrorMessage"] = _locService.GetSystem("User_NotFound");
                return RedirectToAction("Login");

            }
            if (!user.EmailConfirmed)
            {
                TempData["SuccessMessage"] = _locService.GetSystem("Auth_ResendSuccess");
                return RedirectToAction("VerifyEmail");
            }
            TempData["InfoMessage"] = _locService.GetSystem("Auth_EmailVerifiedStatus");
            return RedirectToAction("Login");

        }

        public IActionResult AccessDenied(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                var currentUser = _userManager.GetUserAsync(User).Result;
                var redirectedRoute = GetRedirectedRoute(currentUser);


                return View();
            }
            else
            {
                return RedirectToAction("Login", "Auth", new
                {
                    returnUrl = returnUrl ?? (Request.Path + Request.QueryString),                });
            }
        }
    }
}
