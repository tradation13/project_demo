
using IPTS.Data;
using IPTS.Helpers;
using IPTS.Resources;
using IPTS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IPTS.Areas.Patient.Controllers
{
    [Area("patient")]
    [Authorize(Roles = "patient")]
    public class MedicalCasesController(
    LocService locService,
    MedicalCaseService medicalCaseService,
    MedicalCaseTestPhotoService medicalCaseTestPhotoService,
    PatientService patientService,
    UserService userService,
    MedicalReportService medicalReportService,
    ApplicationDbContext context
    ) : Controller
    {
        
        private readonly LocService _locService = locService;
        private readonly MedicalReportService _medicalReportService = medicalReportService;
    private readonly MedicalCaseService _medicalCaseService = medicalCaseService;
    private readonly MedicalCaseTestPhotoService _medicalCaseTestPhotoService = medicalCaseTestPhotoService;
    private readonly PatientService _patientService = patientService;
    private readonly UserService _userService = userService;
    private readonly ApplicationDbContext _context = context;

        private async Task<int?> GetCurrentPatientIdAsync()
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return null;

            var patient = (await _userService.GetByIdAsync(currentUserId, q => q.Include(u => u.Patient))).Patient;
            return patient?.Id;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            var patient = (await _userService.GetByIdAsync(userId, o => o.Include(q => q.Patient))).Patient;

            if (patient == null) return NotFound();

            var medicalCases = await _medicalCaseService.GetCasesForPatientAsync(patient.Id);

            ViewBag.Patient = patient;
            return View(medicalCases);
        }


        [HttpGet]
        public async Task<IActionResult> ViewReport(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return NotFound();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Forbid();

            var ownsReport = await _context.MedicalReportHistories
                .AnyAsync(r => r.ReportUrl == fileName && r.UserId == currentUserId);

            if (!ownsReport)
                return NotFound();

            var stream = _medicalReportService.OpenReportFile(fileName);
            if (stream == null)
                return NotFound(_locService.GetSystem("Error_ReportFileNotFound"));

            return File(stream, "application/pdf");
        }

        [HttpGet]
        public async Task<IActionResult> PrintReport(int id)
        {
            var currentPatientId = await GetCurrentPatientIdAsync();
            if (!currentPatientId.HasValue)
                return Forbid();

            var medicalCase = await _medicalCaseService.GetCaseForReportAsync(id);
            if (medicalCase == null || medicalCase.PatientId != currentPatientId.Value)
                return NotFound();

            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
            var currentLang = requestCulture?.RequestCulture.Culture.TwoLetterISOLanguageName ?? "en";
            var result = await _medicalReportService.GeneratePdfAsync(
                medicalCase,
                currentLang,
                new HttpUser(HttpContext),
                saveToHistory: false);

            return File(result.PdfBytes, "application/pdf", result.DownloadFileName);
        }

        public async Task<IActionResult> Details(int id)
        {
            var currentPatientId = await GetCurrentPatientIdAsync();
            if (!currentPatientId.HasValue)
                return Forbid();

            var medicalCase = await _medicalCaseService.GetCaseWithTestsAsync(id);
            if (medicalCase == null) return NotFound();
            if (medicalCase.PatientId != currentPatientId.Value) return NotFound();

            var patient = await _patientService.GetByIdAsync(medicalCase.PatientId, q => q.Include(p => p.User));
            ViewBag.Patient = patient;
            return View(medicalCase);
        }

        [HttpGet]
        public async Task<IActionResult> ViewTestPhoto(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return NotFound();

            var currentPatientId = await GetCurrentPatientIdAsync();
            if (!currentPatientId.HasValue)
                return Forbid();

            var photo = await _medicalCaseTestPhotoService.GetByFileNameAsync(fileName);
            if (photo?.MedicalCase == null || photo.MedicalCase.PatientId != currentPatientId.Value)
                return NotFound();

            var opened = _medicalCaseTestPhotoService.OpenPhotoFile(photo.FileName);
            if (opened == null || opened.Value.Stream == null)
                return NotFound();

            return File(opened.Value.Stream, opened.Value.ContentType);
        }
    }
}
