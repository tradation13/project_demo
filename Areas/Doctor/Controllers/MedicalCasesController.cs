using IPTS.Resources;
using IPTS.Helpers;
using IPTS.Models.Entites;
using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;

namespace IPTS.Areas.Doctor.Controllers
{
    [Area("doctor")]
    [Authorize(Roles = "doctor")]
    public class MedicalCasesController(
        LocService locService,
        MedicalCaseService medicalCaseService,
        MedicalCaseTestService medicalCaseTestService,
        MedicalCaseTestPhotoService medicalCaseTestPhotoService,
        PatientService patientService,
        TestService testService,
        UserService userService,
        MedicalReportService medicalReportService) : Controller
    {
        private readonly LocService _locService = locService;
        private readonly MedicalReportService _medicalReportService = medicalReportService;
        private readonly MedicalCaseService _medicalCaseService = medicalCaseService;
        private readonly MedicalCaseTestService _medicalCaseTestService = medicalCaseTestService;
        private readonly MedicalCaseTestPhotoService _medicalCaseTestPhotoService = medicalCaseTestPhotoService;
        private readonly PatientService _patientService = patientService;
        private readonly TestService _testService = testService;
        private readonly UserService _userService = userService;
        public async Task<IActionResult> Index(int patientId)
        {

            var patient = await _patientService.GetByIdAsync(patientId, q => q.Include(p => p.User));
            if (patient == null) return NotFound();

            var medicalCases = await _medicalCaseService.GetCasesForPatientAsync(patientId);

            ViewBag.Patient = patient;
            return View(medicalCases);
        }

        public async Task<IActionResult> Details(int id)
        {
            var medicalCase = await _medicalCaseService.GetCaseWithTestsAsync(id);
            if (medicalCase == null) return NotFound();

            var patient = await _patientService.GetByIdAsync(medicalCase.PatientId, q => q.Include(p => p.User));
            ViewBag.Patient = patient;
            return View(medicalCase);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int patientId)
        {
            var patient = await _patientService.GetByIdAsync(patientId, q => q.Include(p => p.User));
            if (patient == null) return NotFound();

            ViewBag.Patient = patient;
            return View(new MedicalCaseViewModel { PatientId = patientId, CreatedAt = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MedicalCaseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var patient = await _patientService.GetByIdAsync(model.PatientId, q => q.Include(p => p.User));
                ViewBag.Patient = patient;
                return View(model);
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid(); 
            }

            var doctor = (await _userService.GetByIdAsync(userId, o => o.Include(q => q.Doctor))).Doctor;
            if (doctor == null)
            {
                ModelState.AddModelError("", _locService.GetSystem("Error_DoctorProfileNotFound"));
                var patient = await _patientService.GetByIdAsync(model.PatientId, q => q.Include(p => p.User));
                ViewBag.Patient = patient;
                return View(model);
            }

           var entity = new MedicalCase
{
    Name = model.Name,
    Description = model.Description,
    PatientId = model.PatientId,
    DoctorId = doctor.Id,
    DominantSide = model.DominantSide,
    ActivityLevel = model.ActivityLevel,
    InjuryHistory = model.InjuryHistory,
    Medications = model.Medications,
    FunctionalAbility = model.FunctionalAbility,
    PersonalGoals = model.PersonalGoals,
    CreatedAt = model.CreatedAt == default ? DateTime.UtcNow : model.CreatedAt.ToUniversalTime()
};

            await _medicalCaseService.AddAsync(entity);

            return RedirectToAction(nameof(Index), new { patientId = model.PatientId });
        }


        [HttpGet]
        public async Task<IActionResult> AddTest(int medicalCaseId)
        {
            var medicalCase = await _medicalCaseService.GetByIdAsync(medicalCaseId, q => q.Include(mc => mc.Patient).ThenInclude(p => p.User));
            if (medicalCase == null) return NotFound();

            ViewBag.Tests = await _testService.GetAllAsync(q => q.Include(t => t.TestGroup).OrderBy(t => t.Name));
            ViewBag.Patient = medicalCase.Patient;
            return View(new MedicalCaseTestViewModel { MedicalCaseId = medicalCaseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTest(MedicalCaseTestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Tests = await _testService.GetAllAsync(q => q.Include(t => t.TestGroup).OrderBy(t => t.Name));
                var medicalCase = await _medicalCaseService.GetByIdAsync(model.MedicalCaseId, q => q.Include(mc => mc.Patient).ThenInclude(p => p.User));
                ViewBag.Patient = medicalCase?.Patient;
                return View(model);
            }

            await _medicalCaseTestService.AddAsync(model);
            LogHelper.LogWithContext(
                $"Added medical case test '{model.TestName}' (StandardValue={model.StandardValue}) to case {model.MedicalCaseId}",
                User?.Identity?.Name ?? "Unknown",
                "Doctor",
                "MedicalCasesController.AddTest",
                LogEventLevel.Information);
            return RedirectToAction("Details", new { id = model.MedicalCaseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTest(int id)
        {
            var test = await _medicalCaseTestService.GetByIdAsync(id);
            if (test == null) return NotFound();
            var medicalCaseId = test.MedicalCaseId;
            var testId = test.TestId;
            await _medicalCaseTestService.DeleteAsync(id);
            await _medicalCaseTestPhotoService.DeletePileIfOrphanedAsync(medicalCaseId, testId);
            LogHelper.LogWithContext(
                $"Deleted medical case test {id} from case {medicalCaseId}",
                User?.Identity?.Name ?? "Unknown",
                "Doctor",
                "MedicalCasesController.DeleteTest",
                LogEventLevel.Warning);
            return RedirectToAction("Details", new { id = medicalCaseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTestPhoto(int medicalCaseId, int testId, int photoKind, int slot, IFormFile? file)
        {
            var (success, error) = await _medicalCaseTestPhotoService.SaveOrReplaceAsync(
                medicalCaseId, testId, photoKind, slot, file);

            if (!success)
            {
                TempData["ErrorMessage"] = error;
                LogHelper.LogWithContext(
                    $"Failed to upload comparison photo for case {medicalCaseId}, test {testId}, kind {photoKind}, slot {slot}: {error}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "MedicalCasesController.UploadTestPhoto",
                    LogEventLevel.Warning);
            }
            else
            {
                TempData["SuccessMessage"] = _locService.GetSystem("TestPhoto_SaveSuccess");
                LogHelper.LogWithContext(
                    $"Saved comparison photo for case {medicalCaseId}, test {testId}, kind {photoKind}, slot {slot}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "MedicalCasesController.UploadTestPhoto",
                    LogEventLevel.Information);
            }

            return RedirectToAction(nameof(Details), new { id = medicalCaseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTestPhoto(int id)
        {
            var (success, medicalCaseId, error) = await _medicalCaseTestPhotoService.DeleteAsync(id);
            if (!success || !medicalCaseId.HasValue)
            {
                TempData["ErrorMessage"] = error ?? _locService.GetSystem("TestPhoto_NotFound");
                if (medicalCaseId.HasValue)
                    return RedirectToAction(nameof(Details), new { id = medicalCaseId.Value });
                return NotFound();
            }

            LogHelper.LogWithContext(
                $"Deleted comparison photo {id} from case {medicalCaseId.Value}",
                User?.Identity?.Name ?? "Unknown",
                "Doctor",
                "MedicalCasesController.DeleteTestPhoto",
                LogEventLevel.Warning);
            return RedirectToAction(nameof(Details), new { id = medicalCaseId.Value });
        }

        [HttpGet]
        public async Task<IActionResult> ViewTestPhoto(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return NotFound();

            var photo = await _medicalCaseTestPhotoService.GetByFileNameAsync(fileName);
            if (photo == null)
                return NotFound();

            var opened = _medicalCaseTestPhotoService.OpenPhotoFile(photo.FileName);
            if (opened == null || opened.Value.Stream == null)
                return NotFound();

            return File(opened.Value.Stream, opened.Value.ContentType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTestResult(int id, string result, decimal? standardValue)
        {
            if (standardValue.HasValue && (standardValue.Value < -100000m || standardValue.Value > 1000000m))
            {
                TempData["ErrorMessage"] = _locService.GetSystem("InvalidStandardValue");
                var existing = await _medicalCaseTestService.GetByIdAsync(id);
                return RedirectToAction("Details", new { id = existing?.MedicalCaseId });
            }

            var updated = await _medicalCaseTestService.UpdateTestResultAsync(id, result, standardValue);
            if (!updated) return NotFound();
            var test = await _medicalCaseTestService.GetByIdAsync(id);
            LogHelper.LogWithContext(
                $"Updated medical case test {id} (Result={result}, StandardValue={standardValue})",
                User?.Identity?.Name ?? "Unknown",
                "Doctor",
                "MedicalCasesController.UpdateTestResult",
                LogEventLevel.Information);
            return RedirectToAction("Details", new { id = test?.MedicalCaseId });
        }
        [HttpGet]
        public async Task<IActionResult> PrintReport(int id)
        {
            var medicalCase = await _medicalCaseService.GetCaseForReportAsync(id);
            if (medicalCase == null) return NotFound();

            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
            var currentLang = requestCulture?.RequestCulture.Culture.TwoLetterISOLanguageName ?? "en";
            var result = await _medicalReportService.GeneratePdfAsync(
                medicalCase,
                currentLang,
                new HttpUser(HttpContext),
                saveToHistory: true);

            LogHelper.LogWithContext(
                $"Generated medical report for case {id}",
                User?.Identity?.Name ?? "Unknown",
                "Doctor",
                "MedicalCasesController.PrintReport",
                LogEventLevel.Information);

            if (string.Equals(Request.Query["response"], "json", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(result.StoredFileName))
                    return StatusCode(StatusCodes.Status500InternalServerError);

                return Json(new
                {
                    downloadUrl = Url.Action(nameof(DownloadReport), new
                    {
                        fileName = result.StoredFileName,
                        downloadName = result.DownloadFileName
                    }),
                    downloadFileName = result.DownloadFileName
                });
            }

            return File(result.PdfBytes, "application/pdf", result.DownloadFileName);
        }

        [HttpGet]
        public IActionResult DownloadReport(string fileName, string? downloadName = null)
        {
            var stream = _medicalReportService.OpenReportFile(fileName);
            if (stream == null)
                return NotFound(_locService.GetSystem("Error_ReportFileNotFound"));

            var name = string.IsNullOrWhiteSpace(downloadName)
                ? fileName
                : Path.GetFileName(downloadName);
            return File(stream, "application/pdf", name);
        }

        [HttpGet]
        public IActionResult ViewReport(string fileName)
        {
            var stream = _medicalReportService.OpenReportFile(fileName);
            if (stream == null)
                return NotFound(_locService.GetSystem("Error_ReportFileNotFound"));

            return File(stream, "application/pdf");
        }



    }
}
