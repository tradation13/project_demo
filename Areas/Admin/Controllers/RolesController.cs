using IPTS.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IPTS.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = "admin")]
    public class RolesController(RoleManager<IdentityRole> roleManager, IdentityErrorTranslator identityErrorTranslator) : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly IdentityErrorTranslator _identityErrorTranslator = identityErrorTranslator;

        public IActionResult Index()
        {
            var roles = _roleManager.Roles.ToList();
            return View(roles);
        }

        public IActionResult Create()
        {
            return View(new IdentityRole());
        }

        [HttpPost]
        public async Task<IActionResult> Create(IdentityRole model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _roleManager.CreateAsync(new IdentityRole(model.Name));
            if (!result.Succeeded)
            {
                var translatedErrors = _identityErrorTranslator.TranslateErrors(result.Errors);
                ModelState.AddModelError(string.Empty, translatedErrors);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            return View(role);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, IdentityRole model)
        {
            if (!ModelState.IsValid) return View(model);

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            role.Name = model.Name;
            var result = await _roleManager.UpdateAsync(role);

            if (!result.Succeeded)
            {
                var translatedErrors = _identityErrorTranslator.TranslateErrors(result.Errors);
                ModelState.AddModelError(string.Empty, translatedErrors);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                var translatedErrors = _identityErrorTranslator.TranslateErrors(result.Errors);
                TempData["Error"] = translatedErrors;
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}