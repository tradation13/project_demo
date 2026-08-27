using AutoMapper;
using IPTS.Data;
using IPTS.Helpers;
using IPTS.Models.Entites;
using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;
using System.Security.Claims;

namespace IPTS.Controllers
{
    [Route("[controller]")]
    public class ProfileController(
        UserService userService,
        IMapper mapper,
        SpecialtyService specialtyService,
        ApplicationDbContext context) : Controller
    {
        private readonly UserService _userService = userService;
        private readonly SpecialtyService _specialtyService = specialtyService;
        private readonly IMapper _mapper = mapper;
        private readonly ApplicationDbContext _context = context;

        [HttpGet("{id?}")]
        public async Task<IActionResult> Index([FromRoute] string? id)
        {
            var currentUserId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User?.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(currentUserId)) return NotFound();

            id ??= currentUserId;

            var user = await _userService.GetByIdAsync<string>(
                id,
                u => u.Include(a => a.Admin)
                      .Include(a => a.Patient)
                      .Include(a => a.Doctor)
                      .Include(a => a.UserType)
            );

            if (user == null) return NotFound();

            if (!await CanViewProfileAsync(user, currentUserId, userRole))
                return DenyProfile(currentUserId, userRole, id);

            var model = _mapper.Map<UserProfileViewModel>(user);

            if (!string.IsNullOrEmpty(currentUserId))
            {
                var currentUser = await _userService.GetByIdAsync(currentUserId,
                    u => u.Include(x => x.UserType));

                ViewBag.HasDashboard = currentUser?.UserType?.HasDashboard ?? false;
            }

            if (id == currentUserId || IsAdmin(userRole)) ViewBag.CanEdit = true;
            if (model.Doctor != null)
                ViewBag.Specialties = await _specialtyService.GetAllAsync();

            ViewBag.UserName = user.UserName;
            ViewBag.ProfileKind = ResolveProfileKind(user, model, id, currentUserId, userRole);
            return View(model);
        }

        [Authorize]
        [HttpGet("Edit/{id?}")]
        public async Task<IActionResult> Edit(string? id)
        {
            var currentUserId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User?.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(currentUserId)) return NotFound();

            id ??= currentUserId;

            if (id != currentUserId && !IsAdmin(userRole)) return Forbid();

            var user = await _userService.GetByIdAsync<string>(
                            id,
                            u => u.Include(a => a.Admin)
                                  .Include(a => a.Patient)
                                  .Include(a => a.Doctor)
                                  .Include(a => a.UserType)
                        );

            if (user == null) return NotFound();

            var model = _mapper.Map<UserProfileViewModel>(user);

            ViewBag.IsEditMode = true;
            ViewBag.CanEdit = true;

            if (!string.IsNullOrEmpty(currentUserId))
            {
                var currentUser = await _userService.GetByIdAsync(currentUserId,
                    u => u.Include(x => x.UserType));

                ViewBag.HasDashboard = currentUser?.UserType?.HasDashboard ?? false;
            }
            ViewBag.UserName = user.UserName;
            if (model.Doctor != null)
                ViewBag.Specialties = await _specialtyService.GetAllAsync();

            ViewBag.ProfileKind = ResolveProfileKind(user, model, id, currentUserId, userRole);
            return View("Index", model);
        }

        [Authorize]
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserProfileViewModel model)
        {
            var currentUserId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User?.FindFirst(ClaimTypes.Role)?.Value;

            if (id != currentUserId && !IsAdmin(userRole)) return Forbid();

            ViewBag.IsEditMode = true;
            ViewBag.CanEdit = true;

            if (!ModelState.IsValid)
            {
                var user = await _userService.GetByIdAsync<string>(
                    id,
                    u => u.Include(a => a.Admin)
                          .Include(a => a.Patient)
                          .Include(a => a.Doctor)
                          .Include(x => x.UserType));

                if (user == null) return NotFound();

                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var currentUser = await _userService.GetByIdAsync(currentUserId,
                        u => u.Include(x => x.UserType));
                    ViewBag.HasDashboard = currentUser?.UserType?.HasDashboard ?? false;
                }

                ViewBag.UserName = user.UserName;

                if (model.Doctor != null)
                    ViewBag.Specialties = await _specialtyService.GetAllAsync();

                ViewBag.ProfileKind = ResolveProfileKind(user, model, id, currentUserId, userRole);
                return View("Index", model);
            }
            if (model.Doctor != null)
                ViewBag.Specialties = await _specialtyService.GetAllAsync();

            await _userService.UpdateProfileAsync(model);
            return RedirectToAction("Index", new { id });
        }

        private async Task<bool> CanViewProfileAsync(AppUser target, string? viewerId, string? viewerRole)
        {
            if (string.Equals(target.Id, viewerId, StringComparison.OrdinalIgnoreCase))
                return true;

            if (IsAdmin(viewerRole))
                return true;

            if (target.Admin != null)
                return false;

            // Doctor profiles stay public for the Therapies page.
            if (target.Doctor != null)
                return true;

            if (target.Patient == null || !IsDoctor(viewerRole) || string.IsNullOrEmpty(viewerId))
                return false;

            var doctorId = await _context.Doctors
                .Where(d => d.UserId == viewerId)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();

            if (doctorId == null)
                return false;

            var patientId = target.Patient.Id;
            return await _context.Appointments.AnyAsync(a => a.DoctorId == doctorId && a.PatientId == patientId)
                || await _context.MedicalCases.AnyAsync(mc => mc.DoctorId == doctorId && mc.PatientId == patientId);
        }

        private IActionResult DenyProfile(string? viewerId, string? viewerRole, string? targetId)
        {
            LogHelper.LogWithContext(
                $"Profile access denied for user '{targetId}'",
                viewerId ?? "Anonymous",
                viewerRole ?? "Guest",
                "Profile.Index",
                LogEventLevel.Warning);

            if (string.IsNullOrEmpty(viewerId))
                return Challenge();

            return Forbid();
        }

        private static bool IsAdmin(string? role) =>
            string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);

        private static bool IsDoctor(string? role) =>
            string.Equals(role, "doctor", StringComparison.OrdinalIgnoreCase);

        private static string ResolveProfileKind(
            AppUser user,
            UserProfileViewModel model,
            string? profileUserId,
            string? currentUserId,
            string? viewerRole)
        {
            if (model.Doctor != null || user.Doctor != null)
                return "doctor";

            var typeName = user.UserType?.Name?.Trim() ?? "";
            var isAdminType = typeName.Contains("admin", StringComparison.OrdinalIgnoreCase)
                || typeName.Contains("أدمن", StringComparison.OrdinalIgnoreCase);
            var isOwnAdminSession = string.Equals(profileUserId, currentUserId, StringComparison.OrdinalIgnoreCase)
                && IsAdmin(viewerRole);

            if (model.Admin != null || user.Admin != null || isAdminType || isOwnAdminSession)
                return "admin";

            return "patient";
        }
    }
}
