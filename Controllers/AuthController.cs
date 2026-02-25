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
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IPTS.Controllers
{
    public class AuthController(LocService locService,EmailService emailService, ILogger<AuthController> logger, UserManager<AppUser> userManager, UserService userService, SignInManager<AppUser> signInManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context, IdentityErrorTranslator identityErrorTranslator) : Controller
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

        [HttpGet]
        [OutputCache(Duration = 3600)]
        public IActionResult Login(string? returnUrl = null)
        {

            //if (accessDeniedSituation != null )
            //    TempData["ErrorMessage"] = "You are not authorized to access this page";

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUser = _userManager.GetUserAsync(User).Result;
                var redirectedRoute = GetRedirectedRoute(currentUser);


                return RedirectToAction(
                    actionName: redirectedRoute.ActionName,
                    controllerName: redirectedRoute.Controller,
                    routeValues: new { area = redirectedRoute.Area });
            }
            //var u = new AppUser { UserName = "Admin", Email = "muhammadkalumian@gmail.com" };
            //var s = await _userManager.CreateAsync(u, "Kalumian@4002");

            //var adminUser = await _userManager.FindByNameAsync("Admin");
            //await _roleManager.CreateAsync(new IdentityRole("Admin"));
            //await _userManager.AddToRoleAsync(adminUser, "Admin");

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var returnUrl = model.ReturnUrl;

            if (!ModelState.IsValid)
                return View(model);

            AppUser? user = model.UsernameOrEmail.Contains('@')
                ? await _userManager.FindByEmailAsync(model.UsernameOrEmail)
                : await _userManager.FindByNameAsync(model.UsernameOrEmail);

             if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))

            {

                ModelState.AddModelError(string.Empty, _locService.GetSystem("Auth_InvalidLogin"));

                return View(model);

            }

            if (user.Status != EnUserStatus.Active)
            {
                ModelState.AddModelError(string.Empty, _locService.GetSystem("Auth_UserInactive"));
                TempData["ErrorMessage"] = _locService.GetSystem("Auth_UserInactive");
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                TempData["WarningMessage"] = _locService.GetSystem("Auth_EmailNotVerified");

                return RedirectToAction("VerifyEmail", new { email = user.Email});
            }

            await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);

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
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
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
        [OutputCache(Duration = 3600)]
        public async Task<IActionResult> Register()
        {
            var model = new RegisterViewModel();

            return View(model);
        }
        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _userService.RegisterAsync(model);

            if (!result.Succeeded)
            {
                var translatedErrors = _identityErrorTranslator.TranslateErrorsList(result.Errors);
                foreach (var error in translatedErrors)
                    ModelState.AddModelError(string.Empty, error);

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
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = _locService.GetSystem("Auth_VerifySuccess");
                await _signInManager.SignInAsync(user, false);
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
