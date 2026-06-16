using AutoMapper;
using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IPTS.Controllers
{
    [Route("[controller]")]
    public class ProfileController(UserService userService, IMapper mapper, SpecialtyService specialtyService) : Controller
    {
        private readonly UserService _userService = userService;
        private readonly SpecialtyService _specialtyService = specialtyService;
        private readonly IMapper _mapper = mapper;

        [HttpGet("{id?}")]
        public async Task<IActionResult> Index([FromRoute] string? id)
        {
            var currentUserId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User?.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(currentUserId))  return NotFound();

            id ??= currentUserId;

            var user = await _userService.GetByIdAsync<string>(
                id,
                u => u.Include(a => a.Admin)
                      .Include(a => a.Patient)
                      .Include(a => a.Doctor)
                      .Include(a => a.UserType)
            );
            
            var model = _mapper.Map<UserProfileViewModel>(user);
            // Problem
            if (model.Admin != null && userRole != "admin") return Unauthorized();

            if (!string.IsNullOrEmpty(currentUserId))
            {
                var currentUser = await _userService.GetByIdAsync(currentUserId,
                    u => u.Include(x => x.UserType));

                ViewBag.HasDashboard = currentUser.UserType?.HasDashboard ?? false;
            }

            if (id == currentUserId || userRole == "admin") ViewBag.CanEdit = true;
            if (model.Doctor != null)
                ViewBag.Specialties = await _specialtyService.GetAllAsync();

            ViewBag.UserName = user.UserName;
            return View(model);
        }
        [HttpGet("Edit/{id?}")]
        public async Task<IActionResult> Edit(string? id)
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

            var model = _mapper.Map<UserProfileViewModel>(user);


            if (id != currentUserId && userRole != "admin") return Unauthorized();

            ViewBag.IsEditMode = true;
            ViewBag.CanEdit = true;

            if (!string.IsNullOrEmpty(currentUserId))
            {
                var currentUser = await _userService.GetByIdAsync(currentUserId,
                    u => u.Include(x => x.UserType));

                ViewBag.HasDashboard = currentUser.UserType?.HasDashboard ?? false;
            }
            ViewBag.UserName = user.UserName;
            if (model.Doctor != null)
                ViewBag.Specialties = await _specialtyService.GetAllAsync();

            return View("Index", model);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserProfileViewModel model)
        {
            var currentUserId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User?.FindFirst(ClaimTypes.Role)?.Value;

            if (id != currentUserId && userRole != "admin") return Unauthorized();

            ViewBag.IsEditMode = true;
            ViewBag.CanEdit = true;

            if (!ModelState.IsValid)
{
                var user = await _userService.GetByIdAsync<string>(
                    id,
                    u => u.Include(x => x.UserType));

    if (!string.IsNullOrEmpty(currentUserId))
    {
        var currentUser = await _userService.GetByIdAsync(currentUserId,
            u => u.Include(x => x.UserType));
        ViewBag.HasDashboard = currentUser.UserType?.HasDashboard ?? false;
    }

                ViewBag.UserName = user.UserName;

    if (model.Doctor != null)
        ViewBag.Specialties = await _specialtyService.GetAllAsync();

    return View("Index", model);
}
            if (model.Doctor != null)
                ViewBag.Specialties = await _specialtyService.GetAllAsync();

            await _userService.UpdateProfileAsync(model); 
            return RedirectToAction("Index", new { id });
        }
    }
}
