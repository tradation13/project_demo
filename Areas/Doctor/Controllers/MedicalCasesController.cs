using IPTS.Resources;
using IPTS.Data;
using IPTS.Helpers;
using IPTS.Models.Entites;
using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        PatientService patientService,
        TestService testService,
        PdfPrintService pdfPrintService,
        UserService userService,
        MedicalReportService medicalReportService,
        ApplicationDbContext context) : Controller
    {
        private readonly LocService _locService = locService;
        private readonly MedicalReportService _medicalReportService = medicalReportService;
        private readonly MedicalCaseService _medicalCaseService = medicalCaseService;
        private readonly MedicalCaseTestService _medicalCaseTestService = medicalCaseTestService;
        private readonly PatientService _patientService = patientService;
        private readonly TestService _testService = testService;
        private readonly PdfPrintService _pdfPrintService = pdfPrintService;
        private readonly UserService _userService = userService;
        private readonly ApplicationDbContext _context = context;
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
            await _medicalCaseTestService.DeleteAsync(id);
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
        public async Task<IActionResult> UpdateTestResult(int id, string result, decimal? standardValue)
        {
            if (standardValue.HasValue && (standardValue.Value < -100000m || standardValue.Value > 1000000m))
            {
                TempData["Error"] = _locService.GetSystem("InvalidStandardValue");
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
    
    var medicalCase = await _medicalCaseService.GetByIdAsync(
        id,
        q => q
            .Include(mc => mc.Patient).ThenInclude(p => p.User)
            .Include(mc => mc.Doctor).ThenInclude(d => d.User)
            .Include(mc => mc.Doctor).ThenInclude(d => d.Specialty)
            .Include(mc => mc.MedicalCaseTests)
                .ThenInclude(mct => mct.Test)
                    .ThenInclude(t => t.TestGroup)
    );

    if (medicalCase == null) return NotFound();

     var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
    string currentLang = requestCulture?.RequestCulture.Culture.TwoLetterISOLanguageName ?? "en";
    
    string html = await _medicalReportService.GenerateHtmlReport(medicalCase, currentLang);

    
    byte[] pdfBytes = _pdfPrintService.GeneratePdf(new Helpers.HttpUser(HttpContext), html, $"Medical Report - {medicalCase.Name}");

    

    
    string fileName = $"{Guid.NewGuid()}_{medicalCase.Name}.pdf";
    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "InternalStorage", "MedicalReports");

   
    if (!Directory.Exists(folderPath))
    {
        Directory.CreateDirectory(folderPath);
    }

    string filePath = Path.Combine(folderPath, fileName);

    
    await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

    
    var reportHistory = new MedicalReportHistory
    {
        MedicalCaseId = medicalCase.Id,
        UserId = medicalCase.Patient.UserId, 
        ReportUrl = fileName, 
        CreatedAt = DateTime.UtcNow,
    };

    
    _context.MedicalReportHistories.Add(reportHistory);
    await _context.SaveChangesAsync();

   
string filePrefix = (currentLang == "de") ? "MedizinischerBericht" : "MedicalReport";

return File(pdfBytes, "application/pdf", $"{filePrefix}_Case{medicalCase.Id}_{medicalCase.Patient.User.LastName}.pdf");
}

[HttpGet]
public IActionResult ViewReport(string fileName)
{
    if (string.IsNullOrEmpty(fileName)) return NotFound();

    
    var path = Path.Combine(Directory.GetCurrentDirectory(), "InternalStorage", "MedicalReports", fileName);

 
    if (!System.IO.File.Exists(path)) return NotFound(_locService.GetSystem("Error_ReportFileNotFound"));

   
    var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
    
    
    return File(fileStream, "application/pdf");
}



    }
}
