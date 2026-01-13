using IPTS.Data;
using IPTS.Models.Entites;
using IPTS.Models.Enums;
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
    public class AuthController(EmailService emailService, ILogger<AuthController> logger, UserManager<AppUser> userManager, UserService userService, SignInManager<AppUser> signInManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context) : Controller
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly SignInManager<AppUser> _signInManager = signInManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly UserService _userService = userService;
        private readonly EmailService _emailService = emailService;
        private readonly ILogger<AuthController> _logger = logger;
        private readonly ApplicationDbContext _context = context;

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
                ModelState.AddModelError(string.Empty, "Username or password is incorrect.");
                return View(model);
            }

            if (user.Status != EnUserStatus.Active)
            {
                ModelState.AddModelError(string.Empty, "User isn't active, please contact admins.");
                TempData["ErrorMessage"] = "User isn't active, please contact admins.";
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                TempData["WarningMessage"] = "You need to verify your email first.";

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
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                var currentUser = await _userService.GetByIdAsync(currentUserId,
                    u => u.Include(x => x.UserType));
                ViewBag.HasDashboard = currentUser.UserType?.HasDashboard ?? false;
                TempData["ErrorMessage"] = "Ensure";
                return View(model);
            }

            var user = await _userService.GetByIdAsync(currentUserId,
                u => u.Include(x => x.UserType));

            var redirectedRoute = GetRedirectedRoute(user);
            TempData["SuccessMessage"] = "A password was changed successfully";

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
                "Reset your password",
                $"<p>Hello,</p><p>You requested to reset your password.</p>" +
                $"<p>Please click the link below to set a new password:</p>" +
                $"<p><a href='{resetLink}'>Reset Password</a></p>" +
                "<p>If you did not request this, please ignore this email.</p>");

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
            
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            
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
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            TempData["SuccessMessage"] = "A verification email has been sent. Please check your inbox.";
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
                ModelState.AddModelError(string.Empty, "User not found.");
                return View();
            }

            if (user.EmailConfirmed)
            {
                TempData["SuccessMessage"] = "Your email is already verified. You can login now.";
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
                "Email Verification",
                $"Click this link to verify your email: <a href='{confirmationLink}'>Verify Email</a>"
            );
            TempData["SuccessMessage"] = "A verification email has been sent. Please check your inbox to verify your email address.";
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
                TempData["SuccessMessage"] = "Email verified successfully! You can login now.";
                await _signInManager.SignInAsync(user, false);
                var redirectedRoute = GetRedirectedRoute(user);

                return RedirectToAction(
                    actionName: redirectedRoute.ActionName,
                    controllerName: redirectedRoute.Controller,
                    routeValues: new { area = redirectedRoute.Area });

            }

            TempData["ErrorMessage"] = "Email verification failed.";
            return RedirectToAction("Login");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                TempData["ErrorMessage"] = "user wasn't found";
                return RedirectToAction("Login");

            }
            if (!user.EmailConfirmed)
            {
                TempData["SuccessMessage"] = "Verification email resent!";
                return RedirectToAction("VerifyEmail");
            }
            TempData["InfoMessage"] = "Email was verified!";
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
