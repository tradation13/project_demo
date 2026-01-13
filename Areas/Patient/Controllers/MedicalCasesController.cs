using IPTS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IPTS.Areas.Patient.Controllers
{
    [Area("patient")]
    [Authorize(Roles = "patient")]
    public class MedicalCasesController(
        MedicalCaseService medicalCaseService,
        MedicalCaseTestService medicalCaseTestService,
        PatientService patientService,
        TestService testService,
        PdfPrintService pdfPrintService,
        UserService userService) : Controller
    {
        private readonly MedicalCaseService _medicalCaseService = medicalCaseService;
        private readonly MedicalCaseTestService _medicalCaseTestService = medicalCaseTestService;
        private readonly PatientService _patientService = patientService;
        private readonly TestService _testService = testService;
        private readonly PdfPrintService _pdfPrintService = pdfPrintService;
        private readonly UserService _userService = userService;

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

            if (medicalCase == null)
                return NotFound();

            var sb = new System.Text.StringBuilder();

            sb.Append($@"
            <html>
            <head>
                <style>
                    body {{
                        font-family: 'Segoe UI', Arial, sans-serif;
                        color: #1a1a1a;
                        margin: 30px;
                        font-size: 10pt;
                    }}
                    h1, h2, h3 {{
                        color: #004d40;
                        margin-bottom: 6px;
                    }}
                    .header {{
                        text-align: center;
                        margin-bottom: 25px;
                    }}
                    .header img {{
                        width: 150px;
                        margin-bottom: 10px;
                    }}
                    .info-section {{
                        display: flex;
                        justify-content: space-between;
                        margin-bottom: 20px;
                    }}
                    .info-box {{
                        width: 48%;
                        padding: 8px 12px;
                        border: 1px solid #b0bec5;
                        border-radius: 6px;
                        background-color: #f9f9f9;
                    }}
                    .test-group {{
                        margin-top: 25px;
                        border-top: 2px solid #004d40;
                        padding-top: 10px;
                    }}
                    table {{
                        width: 100%;
                        border-collapse: collapse;
                        margin-top: 10px;
                        font-size: 10pt;
                    }}
                    th, td {{
                        border: 1px solid #cfd8dc;
                        padding: 5px;
                    }}
                    th {{
                        background-color: #004d40;
                        color: white;
                        text-align: left;
                    }}
                    .footer {{
                        border-top: 2px solid #004d40;
                        margin-top: 30px;
                        padding-top: 10px;
                        font-size: 9pt;
                        text-align: right;
                        color: #555;
                    }}
                    svg {{
                        margin-top: 10px;
                    }}
                    text {{
                        font-size: 8pt;
                        fill: #004d40;
                    }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <img src='wwwroot/images/logo.png' alt='System Logo' />
                    <h1>Intelegente Patient Tracker System (IPTS)</h1>
                    <h3>Physiotherapy & Rehabilitation Governance Platform</h3>
                </div>

                <h2>Medical Report: {medicalCase?.Name ?? "N/A"}</h2>
                <p><b>Description:</b> {medicalCase?.Description ?? "N/A"}</p>
                <p><b>Created At:</b> {medicalCase?.CreatedAt:yyyy-MM-dd}</p>
                <p><b>Case ID:</b> #{medicalCase?.Id}</p>

                <div class='info-section'>
                    <div class='info-box'>
                        <h3>Doctor Information</h3>
                        <p><b>Name:</b> Dr. {medicalCase?.Doctor?.User?.FirstName} {medicalCase?.Doctor?.User?.LastName}</p>
                        <p><b>Email:</b> {medicalCase?.Doctor?.User?.Email}</p>
                        <p><b>Specialty:</b> {medicalCase?.Doctor?.Specialty?.Name ?? "N/A"}</p>
                    </div>

                    <div class='info-box'>
                        <h3>Patient Information</h3>
                        <p><b>Name:</b> {medicalCase?.Patient?.User?.FirstName} {medicalCase?.Patient?.User?.LastName}</p>
                        <p><b>Identity No:</b> {medicalCase?.Patient?.IdentityNumber}</p>
                        <p><b>Birth Date:</b> {medicalCase?.Patient?.BirthDate:yyyy-MM-dd}</p>
                    </div>
                </div>
            ");

            var groups = medicalCase.MedicalCaseTests
                .GroupBy(mct => mct.Test.TestGroup.Name)
                .OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                sb.Append($@"
                    <div class='test-group'>
                        <h2>Test Group: {group.Key}</h2>
                    ");

                var tests = group.GroupBy(mct => mct.Test.Name);

                foreach (var test in tests)
                {
                    sb.Append($"<h3>Test: {test.Key}</h3>");

                    var orderedTests = test.OrderBy(t => t.CreatedAt).ToList();

                    if (orderedTests.Count > 1)
                    {
                        sb.Append("<table><tr><th>Date</th><th>Result</th></tr>");
                        foreach (var t in orderedTests)
                        {
                            sb.Append($"<tr><td>{t.CreatedAt:yyyy-MM-dd}</td><td>{t.Result}</td></tr>");
                        }
                        sb.Append("</table>");

                        var values = orderedTests.Select(t => double.TryParse(t.Result, out var r) ? r : 0).ToList();
                        var dates = orderedTests.Select(t => t.CreatedAt.ToString("yyyy-MM-dd")).ToList();

                        double width = 400;
                        double height = 150;
                        double padding = 30;

                        double maxVal = values.Max();
                        double minVal = values.Min();

                        var points = values.Select((val, i) => {
                            double x = padding + i * (width - 2 * padding) / (values.Count - 1);
                            double y = height - padding - ((val - minVal) / (maxVal - minVal + 0.01)) * (height - 2 * padding);
                            return $"{x},{y}";
                        });

                        sb.Append($@"
                            <svg width='{width}' height='{height}'>
                                <polyline fill='none' stroke='#26a69a' stroke-width='2' points='{string.Join(" ", points)}' />
                            ");

                        for (int i = 0; i < points.Count(); i++)
                        {
                            var coords = points.ElementAt(i).Split(',');
                            sb.Append($@"<circle cx='{coords[0]}' cy='{coords[1]}' r='3' fill='#004d40' />");
                            sb.Append($@"<text x='{coords[0]}' y='{Convert.ToDouble(coords[1]) - 5}' text-anchor='middle'>{values[i]}</text>");
                            sb.Append($@"<text x='{coords[0]}' y='{height - padding + 15}' text-anchor='middle'>{dates[i]}</text>");
                        }

                        sb.Append("</svg>");
                    }
                    else
                    {
                        var single = orderedTests.First();
                        sb.Append($"<p><b>Date:</b> {single.CreatedAt:yyyy-MM-dd} — <b>Result:</b> {single.Result}</p>");
                    }
                }

                sb.Append("</div>");
            }

            sb.Append($@"
                    <div class='footer'>
                        <p>Generated on {DateTime.Now:yyyy-MM-dd HH:mm}</p>
                        <p>Report generated upon request by user #{medicalCase.PatientId}</p>
                    </div>
                </body>
                </html>
                ");

            byte[] pdfBytes = _pdfPrintService.GeneratePdf(new Helpers.HttpUser(HttpContext), sb.ToString(), $"Medical Report - {medicalCase.Name}");
            return File(pdfBytes, "application/pdf", $"MedicalReport_{medicalCase.Id}.pdf");
        }
        public async Task<IActionResult> Details(int id)
        {
            var medicalCase = await _medicalCaseService.GetCaseWithTestsAsync(id);
            if (medicalCase == null) return NotFound();

            var patient = await _patientService.GetByIdAsync(medicalCase.PatientId, q => q.Include(p => p.User));
            ViewBag.Patient = patient;
            return View(medicalCase);
        }
    }
}
