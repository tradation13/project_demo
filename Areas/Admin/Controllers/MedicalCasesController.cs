using IPTS.Helpers;
using IPTS.Resources;
using IPTS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;

namespace IPTS.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = "admin")]
    public class MedicalCasesController(
        LocService locService,
        MedicalCaseService medicalCaseService,
        MedicalCaseTestPhotoService medicalCaseTestPhotoService,
        PatientService patientService,
        MedicalReportService medicalReportService) : Controller
    {
        private readonly LocService _locService = locService;
        private readonly MedicalCaseService _medicalCaseService = medicalCaseService;
        private readonly MedicalCaseTestPhotoService _medicalCaseTestPhotoService = medicalCaseTestPhotoService;
        private readonly PatientService _patientService = patientService;
        private readonly MedicalReportService _medicalReportService = medicalReportService;

        public async Task<IActionResult> Index(int patientId)
        {
            var patient = await _patientService.GetByIdAsync(patientId, q => q.Include(p => p.User));
            if (patient == null) return NotFound();

            var medicalCases = await _medicalCaseService.GetCasesForPatientAsync(patientId);
            ViewBag.Patient = patient;

            LogHelper.LogWithContext(
                $"Viewed medical cases for patient {patientId}",
                User?.Identity?.Name ?? "Unknown",
                "Admin",
                "MedicalCasesController.Index",
                LogEventLevel.Information);

            return View(medicalCases);
        }

        public async Task<IActionResult> Details(int id)
        {
            var medicalCase = await _medicalCaseService.GetCaseForReportAsync(id);
            if (medicalCase == null) return NotFound();

            ViewBag.Patient = medicalCase.Patient;

            LogHelper.LogWithContext(
                $"Viewed medical case {id}",
                User?.Identity?.Name ?? "Unknown",
                "Admin",
                "MedicalCasesController.Details",
                LogEventLevel.Information);

            return View(medicalCase);
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
                "Admin",
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

            LogHelper.LogWithContext(
                $"Opened medical report file {fileName}",
                User?.Identity?.Name ?? "Unknown",
                "Admin",
                "MedicalCasesController.ViewReport",
                LogEventLevel.Information);

            return File(stream, "application/pdf");
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
    }
}
