using IPTS.Models.Entites;
using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IPTS.Areas.Doctor.Controllers
{
    [Area("doctor")]
    [Authorize(Roles = "doctor")]
    public class MedicalCasesController(
        MedicalCaseService medicalCaseService,
        MedicalCaseTestService medicalCaseTestService,
        PatientService patientService,
        TestService testService,
        PdfPrintService pdfPrintService,
        UserService userService,
        MedicalReportService medicalReportService) : Controller
    {
        private readonly MedicalReportService _medicalReportService = medicalReportService;
        private readonly MedicalCaseService _medicalCaseService = medicalCaseService;
        private readonly MedicalCaseTestService _medicalCaseTestService = medicalCaseTestService;
        private readonly PatientService _patientService = patientService;
        private readonly TestService _testService = testService;
        private readonly PdfPrintService _pdfPrintService = pdfPrintService;
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
                ModelState.AddModelError("", "Doctor profile not found for current user.");
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
    // التعديل هنا: أضفنا كل الحقول الجديدة التي استقبلناها من الموديل
    Weight = model.Weight,
    Height = model.Height,
    BloodGroup = model.BloodGroup,
    DominantSide = model.DominantSide,
    ActivityLevel = model.ActivityLevel,
    IsSmoker = model.IsSmoker,
    HasChronicDisease = model.HasChronicDisease,
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
            return RedirectToAction("Details", new { id = medicalCaseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTestResult(int id, string result)
        {
            var updated = await _medicalCaseTestService.UpdateTestResultAsync(id, result);
            if (!updated) return NotFound();
            var test = await _medicalCaseTestService.GetByIdAsync(id);
            return RedirectToAction("Details", new { id = test?.MedicalCaseId });
        }
[HttpGet]
public async Task<IActionResult> PrintReport(int id)
{
    // 1. جلب البيانات مع كل الـ Includes الضرورية
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

    // 3. تحويل الـ HTML إلى PDF
    byte[] pdfBytes = _pdfPrintService.GeneratePdf(new Helpers.HttpUser(HttpContext), html, $"Medical Report - {medicalCase.Name}");
    
   // تحديد البادئة بناءً على اللغة
string filePrefix = (currentLang == "de") ? "MedizinischerBericht" : "MedicalReport";

return File(pdfBytes, "application/pdf", $"{filePrefix}_Case{medicalCase.Id}_{medicalCase.Patient.User.LastName}.pdf");
}
//         [HttpGet]
//         public async Task<IActionResult> PrintReport(int id)
//         {
//             var medicalCase = await _medicalCaseService.GetByIdAsync(
//                 id,
//                 q => q
//                     .Include(mc => mc.Patient).ThenInclude(p => p.User)
//                     .Include(mc => mc.Doctor).ThenInclude(d => d.User)
//                     .Include(mc => mc.Doctor).ThenInclude(d => d.Specialty)
//                     .Include(mc => mc.MedicalCaseTests)
//                         .ThenInclude(mct => mct.Test)
//                             .ThenInclude(t => t.TestGroup)
//             );

//             if (medicalCase == null)
//                 return NotFound();

//             var sb = new System.Text.StringBuilder();

//             sb.Append($@"
// <html>
// <head>
//     <style>
//         body {{
//             font-family: 'Segoe UI', Arial, sans-serif;
//             color: #1a1a1a;
//             margin: 30px;
//             font-size: 10pt;
//         }}
//         h1, h2, h3 {{
//             color: #004d40;
//             margin-bottom: 6px;
//         }}
//         .header {{
//             text-align: center;
//             margin-bottom: 25px;
//         }}
//         .header img {{
//             width: 150px;
//             margin-bottom: 10px;
//         }}
//         .info-section {{
//             display: flex;
//             justify-content: space-between;
//             margin-bottom: 20px;
//         }}
//         .info-box {{
//             width: 48%;
//             padding: 8px 12px;
//             border: 1px solid #b0bec5;
//             border-radius: 6px;
//             background-color: #f9f9f9;
//         }}
//         .test-group {{
//             margin-top: 25px;
//             border-top: 2px solid #004d40;
//             padding-top: 10px;
//         }}
//         table {{
//             width: 100%;
//             border-collapse: collapse;
//             margin-top: 10px;
//             font-size: 10pt;
//         }}
//         th, td {{
//             border: 1px solid #cfd8dc;
//             padding: 5px;
//         }}
//         th {{
//             background-color: #004d40;
//             color: white;
//             text-align: left;
//         }}
//         .footer {{
//             border-top: 2px solid #004d40;
//             margin-top: 30px;
//             padding-top: 10px;
//             font-size: 9pt;
//             text-align: right;
//             color: #555;
//         }}
//         svg {{
//             margin-top: 10px;
//         }}
//         text {{
//             font-size: 8pt;
//             fill: #004d40;
//         }}
//     </style>
// </head>
// <body>
//     <div class='header'>
//         <img src='wwwroot/images/logo.png' alt='System Logo' />
//         <h1>Physiotech</h1>
//         <h3>Physiotherapy & Rehabilitation Governance Platform</h3>
//     </div>

//     <h2>Medical Report: {medicalCase?.Name ?? "N/A"}</h2>
//     <p><b>Description:</b> {medicalCase?.Description ?? "N/A"}</p>
//     <p><b>Created At:</b> {medicalCase?.CreatedAt:yyyy-MM-dd}</p>
//     <p><b>Case ID:</b> #{medicalCase?.Id}</p>

//     <div class='info-section'>
//         <div class='info-box'>
//             <h3>Doctor Information</h3>
//             <p><b>Name:</b> Dr. {medicalCase?.Doctor?.User?.FirstName} {medicalCase?.Doctor?.User?.LastName}</p>
//             <p><b>Email:</b> {medicalCase?.Doctor?.User?.Email}</p>
//             <p><b>Specialty:</b> {medicalCase?.Doctor?.Specialty?.Name ?? "N/A"}</p>
//         </div>

//         <div class='info-box'>
//             <h3>Patient Information</h3>
//             <p><b>Name:</b> {medicalCase?.Patient?.User?.FirstName} {medicalCase?.Patient?.User?.LastName}</p>
            
//             <p><b>Birth Date:</b> {medicalCase?.Patient?.BirthDate:yyyy-MM-dd}</p>
//         </div>
//     </div>
// ");

//             var groups = medicalCase.MedicalCaseTests
//                 .GroupBy(mct => mct.Test.TestGroup.Name)
//                 .OrderBy(g => g.Key);

//             foreach (var group in groups)
//             {
//                 sb.Append($@"
//     <div class='test-group'>
//         <h2>Test Group: {group.Key}</h2>
//     ");

//                 var tests = group.GroupBy(mct => mct.Test.Name);

//                 foreach (var test in tests)
//                 {
//                     sb.Append($"<h3>Test: {test.Key}</h3>");

//                     var orderedTests = test.OrderBy(t => t.CreatedAt).ToList();

//                     if (orderedTests.Count > 1)
//                     {
//                         // 1. عرض الجدول أولاً
//                         sb.Append("<table><tr><th>Date</th><th>Result</th></tr>");
//                         foreach (var t in orderedTests)
//                         {
//                             sb.Append($"<tr><td>{t.CreatedAt:yyyy-MM-dd}</td><td>{t.Result}</td></tr>");
//                         }
//                         sb.Append("</table>");

//                         // 2. رسم المنحنى بعد الجدول
//                         var values = orderedTests.Select(t => double.TryParse(t.Result, out var r) ? r : 0).ToList();
//                         var dates = orderedTests.Select(t => t.CreatedAt.ToString("yyyy-MM-dd")).ToList();

//                         double width = 400;
//                         double height = 150;
//                         double padding = 30;

//                         double maxVal = values.Max();
//                         double minVal = values.Min();

//                         var points = values.Select((val, i) => {
//                             double x = padding + i * (width - 2 * padding) / (values.Count - 1);
//                             double y = height - padding - ((val - minVal) / (maxVal - minVal + 0.01)) * (height - 2 * padding);
//                             return $"{x},{y}";
//                         });

//                         sb.Append($@"
// <svg width='{width}' height='{height}'>
//     <polyline fill='none' stroke='#26a69a' stroke-width='2' points='{string.Join(" ", points)}' />
// ");

//                         for (int i = 0; i < points.Count(); i++)
//                         {
//                             var coords = points.ElementAt(i).Split(',');
//                             sb.Append($@"<circle cx='{coords[0]}' cy='{coords[1]}' r='3' fill='#004d40' />");
//                             sb.Append($@"<text x='{coords[0]}' y='{Convert.ToDouble(coords[1]) - 5}' text-anchor='middle'>{values[i]}</text>");
//                             sb.Append($@"<text x='{coords[0]}' y='{height - padding + 15}' text-anchor='middle'>{dates[i]}</text>");
//                         }

//                         sb.Append("</svg>");
//                     }
//                     else
//                     {
//                         var single = orderedTests.First();
//                         sb.Append($"<p><b>Date:</b> {single.CreatedAt:yyyy-MM-dd} — <b>Result:</b> {single.Result}</p>");
//                     }
//                 }

//                 sb.Append("</div>");
//             }

//             sb.Append($@"
//     <div class='footer'>
//         <p>Generated on {DateTime.Now:yyyy-MM-dd HH:mm}</p>
//         <p>Report generated upon request by user #{medicalCase.PatientId}</p>
//     </div>
// </body>
// </html>
// ");

//             byte[] pdfBytes = _pdfPrintService.GeneratePdf(new Helpers.HttpUser(HttpContext),sb.ToString(), $"Medical Report - {medicalCase.Name}");
//             return File(pdfBytes, "application/pdf", $"MedicalReport_{medicalCase.Id}.pdf");
//         }


    }
}
