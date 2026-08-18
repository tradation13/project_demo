using System.Text;
using System.Text.Json;
using IPTS.Data;
using IPTS.Helpers;
using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.Resources;
using System.Globalization; // تأكد من إضافة هذا في الأعلى

namespace IPTS.Services
{
    public sealed class MedicalReportPdfResult
    {
        public required byte[] PdfBytes { get; init; }
        public required string DownloadFileName { get; init; }
    }

    public class MedicalReportService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly LocService _loc;
        private readonly string _photosPath;
        private readonly string _reportsPath;
        private readonly ApplicationDbContext _context;
        private readonly PdfPrintService _pdfPrintService;

        public MedicalReportService(
            LocService loc,
            HttpClient httpClient,
            IConfiguration configuration,
            IWebHostEnvironment env,
            ApplicationDbContext context,
            PdfPrintService pdfPrintService)
        {
            _loc = loc;
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"];
            _photosPath = Path.Combine(env.ContentRootPath, "InternalStorage", "MedicalCasePhotos");
            _reportsPath = Path.Combine(env.ContentRootPath, "InternalStorage", "MedicalReports");
            _context = context;
            _pdfPrintService = pdfPrintService;
        }

        private static bool IsGerman(string lang) => lang.StartsWith("de", StringComparison.OrdinalIgnoreCase);

        private static string FormatBloodGroup(string? bloodGroup)
        {
            return bloodGroup switch
            {
                "APositive" => "A+",
                "ANegative" => "A-",
                "BPositive" => "B+",
                "BNegative" => "B-",
                "OPositive" => "O+",
                "ONegative" => "O-",
                "ABPositive" => "AB+",
                "ABNegative" => "AB-",
                _ => bloodGroup ?? string.Empty
            };
        }

        private static string FormatActivityLevel(string? activityLevel, string lang)
        {
            return activityLevel switch
            {
                "Sedentary" => IsGerman(lang) ? "Sitzend" : "Sedentary",
                "Moderate" => IsGerman(lang) ? "Mäßig" : "Moderate",
                "Active" => IsGerman(lang) ? "Aktiv" : "Active",
                "Professional" => IsGerman(lang) ? "Leistungssportler" : "Professional",
                _ => activityLevel ?? string.Empty
            };
        }

public async Task<string> GenerateHtmlReport(MedicalCase medicalCase, string lang = "en")
{
    // 1. طلب تحليل الذكاء الاصطناعي أولاً
    string aiAnalysis = await GetAiAnalysisAsync(medicalCase, lang);

string generalAnalysis = ExtractGeneralAnalysis(aiAnalysis);

if (lang.StartsWith("de", StringComparison.OrdinalIgnoreCase))
{
    generalAnalysis = generalAnalysis
        .Replace("Clinical Explanation:", "Klinische Erklärung:")
        .Replace("Immediate Physiotherapy Recommendations:", "Sofortige physiotherapeutische Empfehlungen:")
        .Replace("Precautions:", "Vorsichtsmaßnahmen:")
        .Replace("Estimated Recovery Timeline:", "Geschätzter Erholungsverlauf:")
        .Replace("Risk Assessment:", "Risikobewertung:")
        .Replace("Red Flags:", "Warnzeichen:");
}

var testAnalyses = ExtractTestAnalyses(aiAnalysis);

    var sb = new StringBuilder();

    // بداية القالب بالتنسيقات الاحترافية والمتجاوبة
sb.Append($@"
<html>
<head>
<meta charset='UTF-8' />
<meta name='viewport' content='width=device-width, initial-scale=1.0' />
<title>{_loc.GetSystem("MedicalReport")}</title>
<style>
    @import url('https://fonts.googleapis.com/css2?family=Roboto:wght@400;700&display=swap');

    body {{ 
        font-family: 'Roboto', Arial, sans-serif; 
        color: #333; 
        margin: 0; 
        padding: 30px; 
        background-color: #fafafa;
        line-height: 1.6; 
    }}

    /* Container */
    .container {{
        max-width: 1200px;
        margin: auto;
        background: #fff;
        padding: 20px;
        border-radius: 10px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    }}

    /* Header */
    .header {{
        text-align: center;
        border-bottom: 3px solid #004d40;
        padding-bottom: 15px;
        margin-bottom: 25px;
    }}

    .header img {{
      width: 350px; /* تم مضاعفة الحجم من 150px */
    height: auto; 
    margin-bottom: 15px;
    display: block; /* لضمان التوسط بشكل صحيح */
    margin-left: auto;
    margin-right: auto;
    }}

    .header h3 {{
        margin: 0;
        color: #004d40;
        opacity: 0.8;
        font-weight: 700;
    }}

    /* Titles */
    h2 {{
        color: #00695c;
        font-weight: 700;
        font-size: 1.5em;
        margin-top: 40px;
        margin-bottom: 15px;
        border-bottom: 2px solid #004d40;
        padding-bottom: 8px;
    }}

    h3 {{
        color: #00796b;
        font-weight: 600;
        font-size: 1.2em;
        margin-top: 30px;
        margin-bottom: 10px;
    }}

    .additional-info-page {{
  margin-top: 60px;
  border-top: 2px dashed #00bfa5;
  display: block;
  width: 100%;
  background: #fff;
  box-sizing: border-box;
  page-break-before: always; /* للطباعة */
  break-before: page;        /* للمتصفحات الحديثة */
}}

 @media print {{
        .additional-info-page {{
            page-break-before: always;
            break-before: page;
        }}
    }}



    /* Info Sections */
    .info-section {{
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    gap: 20px;
}}

.info-box {{
    background: linear-gradient(135deg, #ffffff 0%, #f8fafc 100%);
    border: none;
    border-top: 4px solid #00695c; /* خط علوي نحيف يعطي فخامة */
    border-radius: 12px;
    padding: 20px;
    box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
}}

.info-box h3 {{
    margin-top: 0;
    font-size: 1.1em;
    border-bottom: 1px solid #e2e8f0;
    padding-bottom: 10px;
    display: flex;
    align-items: center;
    gap: 8px;
}}

    .info-box:hover {{
        box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    }}

    /* AI Analysis */
    .ai-section {{
        background-color: #e0f2f1;
        border-left: 6px solid #004d40;
        padding: 20px;
        margin: 40px 0;
        border-radius: 8px;
        box-shadow: 0 2px 4px rgba(0,0,0,0.05);
        page-break-inside: avoid;
    }}

    .ai-title {{
        font-weight: bold;
        font-size: 1.4em;
        color: #004d40;
        display: flex;
        align-items: center;
        margin-bottom: 15px;
    }}

    /* Test groups */
    .test-group {{
        margin-top: 40px;
        border-top: 3px solid #004d40;
        padding-top: 20px;
        page-break-inside: avoid;
    }}

    /* Tables */
    table {{
        width: 100%;
        border-collapse: collapse;
        margin-top: 15px;
        font-size: 0.95em;
    }}

    th, td {{
        border: 1px solid #cfd8dc;
        padding: 8px;
        text-align: left;
    }}

    th {{
        background-color: #004d40;
        color: #fff;
        font-weight: 600;
    }}

    /* AI Disclaimer */
    .ai-disclaimer {{
        margin-top: 48px;
        padding: 18px 20px;
        background: linear-gradient(135deg, #fff8e1 0%, #fff3e0 100%);
        border: 1px solid #ffe0b2;
        border-left: 5px solid #ef6c00;
        border-radius: 10px;
        page-break-inside: avoid;
    }}

    .ai-disclaimer-title {{
        font-weight: 700;
        font-size: 0.95em;
        color: #e65100;
        margin-bottom: 6px;
        letter-spacing: 0.02em;
    }}

    .ai-disclaimer p {{
        margin: 0;
        font-size: 0.88em;
        color: #5d4037;
        line-height: 1.55;
    }}

    /* Footer */
    .footer {{
        border-top: 2px solid #004d40;
        margin-top: 28px;
        padding-top: 10px;
        font-size: 0.8em;
        color: #777;
        text-align: right;
    }}

    .photo-compare-page {{
        page-break-before: always;
        break-before: page;
        page-break-after: always;
        break-after: page;
        page-break-inside: avoid;
        padding-top: 8px;
    }}

    .photo-compare-page h2 {{
        margin-top: 0;
        font-size: 1.25em;
    }}

    .photo-grid {{
        width: 100%;
        border-collapse: separate;
        border-spacing: 10px;
        table-layout: fixed;
    }}

    .photo-row-label {{
        font-size: 13px;
        font-weight: 700;
        color: #00695c;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        padding: 4px 0 2px;
        border: none;
        background: transparent;
    }}

    .photo-cell {{
        width: 50%;
        vertical-align: middle;
        text-align: center;
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 8px;
        padding: 6px;
        height: 250px;
    }}

    .photo-cell img {{
        max-width: 100%;
        max-height: 238px;
        width: auto;
        height: auto;
    }}

    @media print {{
        .photo-compare-page {{
            page-break-before: always;
            break-before: page;
            page-break-after: always;
            break-after: page;
        }}
    }}

    /* Responsive adjustments */
    @media(max-width: 768px) {{
        .info-section {{
            flex-direction: column;
        }}
        .info-box {{
            flex: 1 1 100%;
        }}
    }}
</style>
</head>
<body>
<div class='container'>

    <div class='header'>
        <img src='wwwroot/images/logo.svg' alt='Logo' />
        <h3>{_loc.GetSystem("PlatformTitle")}</h3>
    </div>

    <h2>{_loc.GetSystem("MedicalReport")}: {medicalCase?.Name ?? "N/A"}</h2>
    
    <div class='info-section'>
        <div class='info-box'>
            <h3>{_loc.GetSystem("PatientInformation")}</h3>
            <p><strong>{_loc.GetSystem("Name")}:</strong> {medicalCase?.Patient?.User?.FirstName} {medicalCase?.Patient?.User?.LastName}</p>
            <p><strong>{_loc.GetSystem("BirthDate")}:</strong> {medicalCase?.Patient?.BirthDate:yyyy-MM-dd}</p>
            <p><strong>{_loc.GetSystem("CaseID")}:</strong> #{medicalCase?.Id}</p>
        </div>

        <div class='info-box'>
            <h3>{_loc.GetSystem("ClinicalDetails")}</h3>
            <p><strong>{_loc.GetSystem("Doctor")}:</strong> Dr. {medicalCase?.Doctor?.User?.LastName}</p>
            <p><strong>{_loc.GetSystem("Date")}:</strong> {medicalCase?.CreatedAt:yyyy-MM-dd}</p>
            <p><strong>{_loc.GetSystem("Description")}:</strong> {medicalCase?.Description}</p>
        </div>

        {(medicalCase.Patient?.Height.HasValue == true || medicalCase.Patient?.Weight.HasValue == true || medicalCase.Patient?.BloodGroup != null ? $@"
        <div class='info-box'>
            <h3>{_loc.GetSystem("PhysicalMeasurements")}</h3>
            {(medicalCase.Patient.Height.HasValue ? $"<p><strong>{_loc.GetSystem("Height")}:</strong> {medicalCase.Patient.Height} cm</p>" : "")}
            {(medicalCase.Patient.Weight.HasValue ? $"<p><strong>{_loc.GetSystem("Weight")}:</strong> {medicalCase.Patient.Weight} kg</p>" : "")}
            {(medicalCase.Patient.BloodGroup != null ? $"<p><strong>{_loc.GetSystem("BloodGroup")}:</strong> {FormatBloodGroup(medicalCase.Patient.BloodGroup.ToString())}</p>" : "")}
        </div>" : "")}
        {(medicalCase.Patient?.IsSmoker.HasValue == true || medicalCase.Patient?.HasChronicDisease.HasValue == true || medicalCase.ActivityLevel != null ? $@"
        <div class='info-box'>
            <h3>{_loc.GetSystem("HealthHistory")}</h3>
            {(medicalCase.Patient.IsSmoker.HasValue ? $"<p><strong>{_loc.GetSystem("IsSmoker")}:</strong> {(medicalCase.Patient.IsSmoker == true ? _loc.GetSystem("Yes") : _loc.GetSystem("No"))}</p>" : "")}
            {(medicalCase.Patient.HasChronicDisease.HasValue ? $"<p><strong>{_loc.GetSystem("ChronicDiseases")}:</strong> {(medicalCase.Patient.HasChronicDisease == true ? _loc.GetSystem("Yes") : _loc.GetSystem("No"))}</p>" : "")}
            {(medicalCase.ActivityLevel != null ? $"<p><strong>{_loc.GetSystem("ActivityLevel")}:</strong> {FormatActivityLevel(medicalCase.ActivityLevel.ToString(), lang)}</p>" : "")}
        </div>" : "")}
    </div>

    {(!string.IsNullOrWhiteSpace(medicalCase.InjuryHistory) || !string.IsNullOrWhiteSpace(medicalCase.Medications) || !string.IsNullOrWhiteSpace(medicalCase.FunctionalAbility) || !string.IsNullOrWhiteSpace(medicalCase.PersonalGoals) ? $@"
    <div class='info-box additional-info-page'>
        <h3>{_loc.GetSystem("AdditionalPatientInfo")}</h3>
        {(!string.IsNullOrWhiteSpace(medicalCase.InjuryHistory) ? $"<p><strong>{_loc.GetSystem("InjuryHistory")}:</strong> {medicalCase.InjuryHistory}</p>" : "")}
        {(!string.IsNullOrWhiteSpace(medicalCase.Medications) ? $"<p><strong>{_loc.GetSystem("Medications")}:</strong> {medicalCase.Medications}</p>" : "")}
        {(!string.IsNullOrWhiteSpace(medicalCase.FunctionalAbility) ? $"<p><strong>{_loc.GetSystem("FunctionalAbility")}:</strong> {medicalCase.FunctionalAbility}</p>" : "")}
        {(!string.IsNullOrWhiteSpace(medicalCase.PersonalGoals) ? $"<p><strong>{_loc.GetSystem("PersonalGoals")}:</strong> {medicalCase.PersonalGoals}</p>" : "")}
    </div>" : "")}

    <div class='ai-section'>
        <div class='ai-title'>🤖 {_loc.GetSystem("SmartAnalysisTitle")}</div>
        <p>{generalAnalysis.Replace("\n", "<br/>")}</p>
    </div>");

            if (medicalCase.MedicalCaseTests != null && medicalCase.MedicalCaseTests.Any())
            {
                var groups = medicalCase.MedicalCaseTests
                    .GroupBy(mct => mct.Test.TestGroup.Name)
                    .OrderBy(g => g.Key);

                foreach (var group in groups)
                {
                    // استخدام GetSystem لترجمة "Test Group"
                    sb.Append($@"<div class='test-group'><h2>{_loc.GetSystem("TestGroup")}: {group.Key}</h2>");
                    
                    foreach (var test in group.GroupBy(mct => mct.Test.Name))
                    {
                        // استخدام GetSystem لترجمة "Test"
                       sb.Append($@"<h3>{_loc.GetSystem("Test")}: {test.Key}</h3>");

var testName = test.Key.Trim();

if (testAnalyses.ContainsKey(testName))
{
                        AppendTestAnalysisBox(sb, testAnalyses[testName], lang);
}
                        var orderedTests = test.OrderBy(t => t.CreatedAt).ToList();
                        
                        // استدعاء ميثود الرسم البياني والجدول
                        AppendTestVisuals(sb, orderedTests);
                    }
                    sb.Append("</div>");
                }
            }

            AppendPhotoComparePages(sb, medicalCase);

    // تنويه الذكاء الاصطناعي + الفوتر
    sb.Append($@"
    <div class='ai-disclaimer'>
        <div class='ai-disclaimer-title'>{_loc.GetSystem("AI_DisclaimerTitle")}</div>
        <p>{_loc.GetSystem("AI_DisclaimerText")}</p>
    </div>
    <div class='footer'>{_loc.GetSystem("GeneratedOn")} {DateTime.Now:yyyy-MM-dd HH:mm}</div>
</div></body></html>");

    return sb.ToString();
}
private async Task<string> GetAiAnalysisAsync(MedicalCase medicalCase, string lang = "en")
{
    try
    {
        Console.WriteLine("\n--- [HUGGING FACE ROUTER REQUEST START] ---");
        
        // 1. تحديد اسم اللغة بالكامل لإرشاد الموديل بدقة
        string targetLanguageName = lang.StartsWith("de", StringComparison.OrdinalIgnoreCase) ? "German" : "English";
        // 1. الرابط الجديد من الـ curl
        var url = "https://api.openai.com/v1/chat/completions";
        
  // 1. بناء بيانات المريض البدنية والتاريخ الصحي ديناميكياً (فقط للقيم الموجودة)
var vitalsSb = new StringBuilder();
if (medicalCase.Patient?.Height.HasValue == true) vitalsSb.AppendLine($"- Height: {medicalCase.Patient.Height} cm");
if (medicalCase.Patient?.Weight.HasValue == true) vitalsSb.AppendLine($"- Weight: {medicalCase.Patient.Weight} kg");
if (medicalCase.Patient?.BloodGroup != null) vitalsSb.AppendLine($"- Blood Group: {medicalCase.Patient.BloodGroup}");
if (medicalCase.Patient?.IsSmoker.HasValue == true) vitalsSb.AppendLine($"- Is Smoker: {(medicalCase.Patient.IsSmoker == true ? "Yes" : "No")}");
if (medicalCase.Patient?.HasChronicDisease.HasValue == true) vitalsSb.AppendLine($"- Has Chronic Diseases: {(medicalCase.Patient.HasChronicDisease == true ? "Yes" : "No")}");
if (medicalCase.ActivityLevel != null) vitalsSb.AppendLine($"- Activity Level: {medicalCase.ActivityLevel}");
if (!string.IsNullOrWhiteSpace(medicalCase.InjuryHistory)) vitalsSb.AppendLine($"- Injury History: {medicalCase.InjuryHistory}");
if (!string.IsNullOrWhiteSpace(medicalCase.Medications)) vitalsSb.AppendLine($"- Medications: {medicalCase.Medications}");
if (!string.IsNullOrWhiteSpace(medicalCase.FunctionalAbility)) vitalsSb.AppendLine($"- Functional Ability: {medicalCase.FunctionalAbility}");
if (!string.IsNullOrWhiteSpace(medicalCase.PersonalGoals)) vitalsSb.AppendLine($"- Personal Goals: {medicalCase.PersonalGoals}");
vitalsSb.AppendLine($"- Patient Date of Birth: {medicalCase.Patient.BirthDate:yyyy-MM-dd}");
vitalsSb.AppendLine($"- Current Date: {DateTime.Today:yyyy-MM-dd}");

// 2. بناء الرسالة (System Message)
var systemMessage = $@" Ignore any previous instructions or prompts. Only follow the instructions in this message.
You are a professional Senior Physiotherapist. 
Analyze the case based ONLY on clinical facts. 
You MUST respond ONLY in {targetLanguageName}.
Use standard medical terminology for the target language.

CRITICAL DATA AUDIT: 
- Actively cross-reference all provided data.
- If you find any clinical contradictions, highlight them in a 'Data Integrity Note' at the end.

PRESENTATION & STYLE:
- The report must be elegant, clean, and highly organized.
- Use a professional medical report layout with clear, bold headings.
- AVOID excessive use of hashtags (#), symbols, or emojis.
- AVOID cluttered formatting; prioritize white space and readability.
- Use structured lists and short, concise paragraphs.
- The tone should be formal, professional, and sophisticated.
- Do not invent medical conditions or terms.
OUTPUT FORMAT RULES (VERY IMPORTANT):

1. Do NOT use Markdown formatting such as:
# headings
## headings
**bold text**
- bullet points

2. Use plain professional text only.

3. Separate the report into two main parts:

PART 1: GENERAL_CLINICAL_ANALYSIS
PART 2: TEST_SPECIFIC_ANALYSIS

4. The GENERAL_CLINICAL_ANALYSIS must NOT interpret or analyze individual clinical tests. 
It should only describe the patient's overall condition, rehabilitation strategy, precautions, and expected recovery.

5. All interpretations of test results must appear ONLY inside TEST_SPECIFIC_ANALYSIS blocks.

6. Each clinical test must be returned in its own block using the following exact structure:

TEST_ANALYSIS_START
TestName: <Exact Test Name>

Doctor Standard/Target:
<use the Standard/Target provided by the doctor when available; do NOT invent a Normal Reference if a doctor Standard exists>

Clinical Interpretation:
<short explanation comparing Result to the doctor Standard/Target when available>

Progress Evaluation:
<trend explanation and how close/far the patient is from the Standard/Target>

Physiotherapy Focus:
<specific therapy advice>

TEST_ANALYSIS_END";

// 3. بناء الرسالة (User Content) مع دمج البيانات الحيوية والنتائج المخبرية
var testResultsLines = medicalCase.MedicalCaseTests.Select(t =>
{
    var standardPart = t.StandardValue.HasValue
        ? $" | Standard/Target: {t.StandardValue.Value.ToString(CultureInfo.InvariantCulture)}"
        : "";
    return $"{t.Test?.Name}: {t.Result}{standardPart}";
});

var userContent = $@"
Please analyze the following medical case and provide the response in {targetLanguageName}:
- Patient Condition: {medicalCase.Description}

{(vitalsSb.Length > 0 ? "- Patient Physical Info & History:\n" + vitalsSb.ToString() : "")}

- Clinical Test Results (Progress Metrics): 
  {string.Join(", ", testResultsLines)}

IMPORTANT INSTRUCTIONS:
- The 'Result' values are progress measurements (degrees/percentages). 
- When 'Standard/Target' is provided, treat it as the doctor-defined goal for that test in this case. Compare Result against that Standard/Target and evaluate proximity to the goal.
- Do NOT invent generic 'Normal Reference' ranges when a Standard/Target is already provided for a test.
- Compare results across dates to identify the recovery trend (e.g., improvement in Range of Motion or Strength).
- If 'Has Chronic Diseases' is true, be more cautious with exercise intensity recommendations.
- Avoid literal English-to-{targetLanguageName} translations of idioms; use professional clinical phrasing.

Please provide:
1. A brief clinical explanation.
2. Immediate physiotherapy recommendations based on the trend.
3. Precautions or activities to avoid.
4. Estimated Recovery Timeline.
5. Risk Assessment & Critical Warnings.
6. Red Flags (Emergency Signs).



Please provide the response using this structure :

GENERAL_CLINICAL_ANALYSIS

Clinical Explanation:
Explain the overall rehabilitation context without analyzing specific tests.

Immediate Physiotherapy Recommendations:
Provide general therapy guidance.

Precautions:
List precautions.

Estimated Recovery Timeline:
Provide estimated recovery time.

Risk Assessment:
Mention potential risks.

Red Flags:
List emergency warning signs.

After that section, provide test-specific analysis blocks.

TEST_SPECIFIC_ANALYSIS

For each clinical test listed above, generate a separate analysis block using the format:

TEST_ANALYSIS_START
TestName: <Exact Test Name>

Doctor Standard/Target:
<use provided Standard/Target when available; otherwise note that no doctor target was set>

Clinical Interpretation:
<short explanation comparing Result to Standard/Target when available>

Progress Evaluation:
<trend and distance to target>

Physiotherapy Focus:
<specific therapy advice>

TEST_ANALYSIS_END

Important rules:
- Do NOT repeat test analysis in the general section.
- Do NOT use Markdown symbols (#, **, -, etc).
- Only plain structured text.
- Prefer doctor Standard/Target over invented normal reference ranges.";
       
       // 3. تجهيز الـ JSON المتوافق مع OpenAI
var requestBody = new
{
    model = "gpt-4o-mini", // تم التغيير لموديل OpenAI
    messages = new[]
    {
        new { role = "system", content = systemMessage },
        new { role = "user", content = userContent }
    },
    stream = false,
    max_tokens = 1200, // رفعنا القيمة لضمان اكتمال التحليل المفصل
    temperature = 0.2 // قيمة منخفضة تجعل الموديل أكثر دقة وواقعية وأقل ميلاً للهبد
};

var jsonRequest = JsonSerializer.Serialize(requestBody);
var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

// 4. إضافة التوكن (تأكد أن _apiKey هو المفتاح الذي يبدأ بـ sk-)
_httpClient.DefaultRequestHeaders.Clear();
_httpClient.DefaultRequestHeaders.Authorization = 
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

// 5. الإرسال
var response = await _httpClient.PostAsync(url, content); // ملاحظة: تأكد أن متغير url أصبح "https://api.openai.com/v1/chat/completions"
var responseBody = await response.Content.ReadAsStringAsync();

if (response.IsSuccessStatusCode)
{
    using var doc = JsonDocument.Parse(responseBody);
    // هيكل الرد في OpenAI هو نفسه القياسي: choices[0].message.content
    var aiText = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

// أضف هذا السطر لمراقبة التكلفة فوراً
var usage = doc.RootElement.GetProperty("usage");
int prompt = usage.GetProperty("prompt_tokens").GetInt32();
int completion = usage.GetProperty("completion_tokens").GetInt32();

Console.WriteLine($"[Cost Monitor] Prompt: {prompt}, Completion: {completion} | Total: {prompt + completion}");
    Console.WriteLine("OpenAI Analysis received successfully!");
    return aiText?.Trim() ?? _loc.GetSystem("AI_NoAnalysisContent");
}
        
        Console.WriteLine($"API Error: {response.StatusCode} - {responseBody}");
        return _loc.GetSystem("AI_TemporarilyUnavailable");
    }
    catch (Exception ex) 
    {
        Console.WriteLine($"Critical Error: {ex.Message}");
        return _loc.GetSystem("AI_ConnectionFailed");
    }
}

private void AppendTestVisuals(StringBuilder sb, List<MedicalCaseTest> tests)
{
    if (tests == null || !tests.Any()) return;

    var inv = CultureInfo.InvariantCulture;
    var values = tests.Select(t => double.TryParse(t.Result, NumberStyles.Any, inv, out var r)
        ? r
        : (double.TryParse(t.Result, NumberStyles.Any, CultureInfo.CurrentCulture, out var r2) ? r2 : 0.0)).ToList();

    double maxVal = values.Max();
    double minVal = values.Min();
    double avgVal = values.Average();
    double? standardValue = tests
        .Where(t => t.StandardValue.HasValue)
        .OrderByDescending(t => t.CreatedAt)
        .Select(t => (double?)t.StandardValue!.Value)
        .FirstOrDefault();

    if (standardValue.HasValue)
    {
        maxVal = Math.Max(maxVal, standardValue.Value);
        minVal = Math.Min(minVal, standardValue.Value);
    }

    // 1. بطاقات الإحصائيات العلوية
    sb.Append(@"<div style='display: grid; grid-template-columns: repeat(auto-fit, minmax(120px, 1fr)); gap: 15px; margin-bottom: 25px; font-family: sans-serif;'>");
    AppendStatCard(sb, _loc.GetSystem("Highest"), values.Max().ToString("F1", inv), "#e8f5e9", "#2e7d32");
    AppendStatCard(sb, _loc.GetSystem("Lowest"), values.Min().ToString("F1", inv), "#ffebee", "#c62828");
    AppendStatCard(sb, _loc.GetSystem("Average"), avgVal.ToString("F1", inv), "#e3f2fd", "#1565c0");
    if (standardValue.HasValue)
        AppendStatCard(sb, _loc.GetSystem("StandardValue"), standardValue.Value.ToString("F1", inv), "#fff8e1", "#f57f17");
    sb.Append("</div>");

    // 2. قسم مدى التحسن (New: Improvement Summary)
    if (tests.Count > 1)
    {
        double firstVal = values.First();
        double lastVal = values.Last();
        double diff = lastVal - firstVal;
        double percent = (firstVal != 0) ? (diff / firstVal) * 100 : 0;
        
        string statusText = diff > 0 ? _loc.GetSystem("Increased") : (diff < 0 ? _loc.GetSystem("Decreased") : _loc.GetSystem("Stable"));
        string statusColor = diff > 0 ? "#2e7d32" : (diff < 0 ? "#c62828" : "#1565c0");
        string arrow = diff > 0 ? "↑" : (diff < 0 ? "↓" : "↔");

        string distanceHtml = "";
        if (standardValue.HasValue)
        {
            var gap = Math.Abs(lastVal - standardValue.Value);
            distanceHtml = $"<div style='font-size:13px; color:#f57f17; margin-top:6px;'>{_loc.GetSystem("DistanceToTarget")}: {gap.ToString("F1", inv)}</div>";
        }

        sb.Append($@"
            <div style='background:#fcfcfc; border:1px solid #eee; padding:15px; border-radius:12px; margin-bottom:25px; font-family:sans-serif; display:flex; align-items:center; justify-content:space-between;'>
                <div>
                    <div style='font-size:12px; color:#7f8c8d; font-weight:bold; text-transform:uppercase;'>{_loc.GetSystem("OverallProgress")}</div>
                    <div style='font-size:18px; font-weight:bold; color:#2c3e50; margin-top:5px;'>
                        {statusText} {_loc.GetSystem("By")} <span style='color:{statusColor};'>{Math.Abs(percent).ToString("F1", inv)}%</span> {arrow}
                    </div>
                    {distanceHtml}
                </div>
                <div style='text-align:right;'>
                    <div style='font-size:11px; color:#95a5a6;'>{_loc.GetSystem("SinceFirstTest")} ({tests.First().CreatedAt:yyyy-MM-dd})</div>
                </div>
            </div>");

        // 3. إعدادات الرسم البياني (SVG)
        var dates = tests.Select(t => t.CreatedAt.ToString("MM-dd")).ToList();
        double width = 600; 
        double height = 250;
        double padding = 50;
        double chartMax = Math.Max(values.Max(), standardValue ?? values.Max());
        double chartMin = Math.Min(values.Min(), standardValue ?? values.Min());
        double range = (chartMax - chartMin) == 0 ? 1 : (chartMax - chartMin);

        var pointsList = new List<string>();
        for (int i = 0; i < values.Count; i++)
        {
            double x = padding + i * (width - 2 * padding) / (values.Count - 1);
            double y = height - padding - ((values[i] - chartMin) / range) * (height - 2 * padding);
            pointsList.Add($"{x.ToString(inv)},{y.ToString(inv)}");
        }

        string standardLineSvg = "";
        if (standardValue.HasValue)
        {
            double sy = height - padding - ((standardValue.Value - chartMin) / range) * (height - 2 * padding);
            standardLineSvg = $@"
                <line x1='{padding.ToString(inv)}' y1='{sy.ToString(inv)}' x2='{(width - padding).ToString(inv)}' y2='{sy.ToString(inv)}'
                      stroke='#f59e0b' stroke-width='2' stroke-dasharray='8,4' />
                <text x='{(width - padding + 4).ToString(inv)}' y='{(sy + 4).ToString(inv)}' font-size='10' fill='#f57f17' font-weight='bold'>{_loc.GetSystem("StandardShort")}: {standardValue.Value.ToString("F1", inv)}</text>";
        }

        sb.Append($@"
            <div style='text-align:center; margin-top:10px; padding:15px; background:#fff; border:1px solid #eee; border-radius:12px; shadow: 0 4px 15px rgba(0,0,0,0.03);'>
                <svg width='100%' height='{height}' viewBox='0 0 {width} {height}' style='font-family:Arial, sans-serif; overflow:visible;'>
                    <line x1='{padding.ToString(inv)}' y1='{padding.ToString(inv)}' x2='{(width - padding).ToString(inv)}' y2='{padding.ToString(inv)}' stroke='#f5f5f5' stroke-dasharray='5,5'/>
                    {standardLineSvg}
                    <polyline fill='none' stroke='url(#lineGradient)' stroke-width='4' stroke-linecap='round' stroke-linejoin='round' points='{string.Join(" ", pointsList)}' />
                    <defs>
                        <linearGradient id='lineGradient' x1='0%' y1='0%' x2='100%' y2='0%'>
                            <stop offset='0%' stop-color='#00bfa5'/><stop offset='100%' stop-color='#00695c'/>
                        </linearGradient>
                    </defs>");

        for (int i = 0; i < pointsList.Count; i++)
        {
            var coords = pointsList[i].Split(',');
            double cx = double.Parse(coords[0], inv);
            double cy = double.Parse(coords[1], inv);

            sb.Append($@"
                <circle cx='{cx.ToString(inv)}' cy='{cy.ToString(inv)}' r='6' fill='white' stroke='#00796b' stroke-width='3' />
                <text x='{cx.ToString(inv)}' y='{(cy - 18).ToString(inv)}' text-anchor='middle' font-size='11' font-weight='bold' fill='#004d40'>{values[i].ToString(inv)}</text>
                <text x='{cx.ToString(inv)}' y='{(height - 10).ToString(inv)}' text-anchor='middle' font-size='10' fill='#999'>{dates[i]}</text>");

            if (i > 0)
            {
                string icon = "";
                if (values[i] > values[i - 1])
                    icon = "<path d='M-4 2 L0 -2 L4 2' fill='none' stroke='#2e7d32' stroke-width='2.5' stroke-linecap='round'/>";
                else if (values[i] < values[i - 1])
                    icon = "<path d='M-4 -2 L0 2 L4 -2' fill='none' stroke='#c62828' stroke-width='2.5' stroke-linecap='round'/>";
                else
                    icon = "<line x1='-4' y1='-1' x2='4' y2='-1' stroke='#1565c0' stroke-width='2' stroke-linecap='round'/><line x1='-4' y1='2' x2='4' y2='2' stroke='#1565c0' stroke-width='2' stroke-linecap='round'/>";

                sb.Append($@"<g transform='translate({cx.ToString(inv)}, {(cy - 38).ToString(inv)})'>{icon}</g>");
            }
        }
        sb.Append("</svg></div>");

        // 4. جدول النتائج
        sb.Append("<div style='margin-top:20px; border:1px solid #eee; border-radius:12px; overflow:hidden;'>");
        sb.Append("<table style='width:100%; border-collapse:collapse; font-family:Arial, sans-serif;'>");
        sb.Append($@"<tr style='background-color:#fcfcfc; border-bottom:1px solid #eee; color:#546e7a; font-size:12px;'>
                        <th style='padding:12px; text-align:left;'>{_loc.GetSystem("Date")}</th>
                        <th style='padding:12px; text-align:center;'>{_loc.GetSystem("Result")}</th>
                        <th style='padding:12px; text-align:center;'>{_loc.GetSystem("StandardShort")}</th>
                    </tr>");
        foreach (var t in tests) 
        {
            var stdCell = t.StandardValue.HasValue
                ? t.StandardValue.Value.ToString("G29", inv)
                : "—";
            sb.Append($@"<tr style='border-bottom:1px solid #f9f9f9;'>
                         <td style='padding:10px; color:#666;'>{t.CreatedAt:yyyy-MM-dd}</td>
                         <td style='padding:10px; text-align:center; font-weight:bold; color:#00695c;'>{t.Result}</td>
                         <td style='padding:10px; text-align:center; font-weight:bold; color:#f57f17;'>{stdCell}</td></tr>");
        }
        sb.Append("</table></div>");
    }
    else
    {
        var single = tests.First();
        string standardBlock = "";
        if (standardValue.HasValue)
        {
            var gap = Math.Abs(values.First() - standardValue.Value);
            standardBlock = $@"<div style='margin-top:10px; color:#f57f17; font-size:0.95em;'>
                {_loc.GetSystem("StandardValue")}: <strong>{standardValue.Value.ToString("F1", inv)}</strong>
                · {_loc.GetSystem("DistanceToTarget")}: <strong>{gap.ToString("F1", inv)}</strong>
            </div>";
        }

        sb.Append($@"<div style='padding:25px; background:#e0f2f1; border-radius:12px; text-align:center; margin-top:20px;'>
                        <div style='color:#00796b; font-size:1.1em;'>{_loc.GetSystem("LatestResult")}</div>
                        <div style='font-size:2.5em; font-weight:bold; color:#004d40;'>{single.Result}</div>
                        {standardBlock}
                        <div style='color:#546e7a;'>📅 {single.CreatedAt:yyyy-MM-dd}</div>
                    </div>");
    }
}

private void AppendStatCard(StringBuilder sb, string label, string value, string bgColor, string textColor)
{
    sb.Append($@"<div style='background:{bgColor}; padding:15px; border-radius:12px; text-align:center; border:1px solid rgba(0,0,0,0.03);'>
                    <div style='font-size:10px; color:{textColor}; text-transform:uppercase; font-weight:bold; opacity:0.8;'>{label}</div>
                    <div style='font-size:20px; font-weight:850; color:{textColor}; margin-top:4px;'>{value}</div>
                </div>");
}

private Dictionary<string, string> ExtractTestAnalyses(string aiText)
{
    var result = new Dictionary<string, string>();

    if (string.IsNullOrWhiteSpace(aiText))
        return result;

    var matches = System.Text.RegularExpressions.Regex.Matches(
        aiText,
        @"TEST_ANALYSIS_START(.*?)TEST_ANALYSIS_END",
        System.Text.RegularExpressions.RegexOptions.Singleline);

    foreach (System.Text.RegularExpressions.Match match in matches)
    {
        var block = match.Groups[1].Value.Trim();

        var lines = block.Split('\n');

        var testLine = lines.FirstOrDefault(l => l.StartsWith("TestName"));

        if (testLine != null)
        {
            var testName = testLine.Replace("TestName:", "").Trim();

            var analysis = block.Replace(testLine, "").Trim();

            result[testName] = analysis;
        }
    }

    return result;
}


private void AppendTestAnalysisBox(StringBuilder sb, string analysis, string lang)
{
    sb.Append($@"
    <div style='
        background:#f8fafc;
        border:1px solid #e2e8f0;
        border-left:6px solid #00bfa5;
        padding:18px;
        border-radius:12px;
        margin-bottom:20px;
        font-family:sans-serif;
        line-height:1.6;
        color:#2c3e50;
        box-shadow:0 4px 12px rgba(0,0,0,0.04);
    '>
        <div style='font-weight:bold; font-size:15px; margin-bottom:8px; color:#00695c;'>
            {(lang.StartsWith("de", StringComparison.OrdinalIgnoreCase) ? "KI-Klinikanalyse" : "Clinical AI Analysis")}
        </div>
        <div style='font-size:14px;'>
            {analysis.Replace("\n","<br/>")}
        </div>
    </div>");
}

private string ExtractGeneralAnalysis(string aiText)
{
    if (string.IsNullOrWhiteSpace(aiText))
        return "";

    var split = aiText.Split("TEST_SPECIFIC_ANALYSIS");

    if (split.Length > 0)
        return split[0].Replace("GENERAL_CLINICAL_ANALYSIS", "").Trim();

    return aiText;
}

private void AppendPhotoComparePages(StringBuilder sb, MedicalCase medicalCase)
{
    var photos = medicalCase.TestPhotos;
    if (photos == null || photos.Count == 0)
        return;

    var testNames = (medicalCase.MedicalCaseTests ?? Enumerable.Empty<MedicalCaseTest>())
        .Where(t => t.Test != null)
        .GroupBy(t => t.TestId)
        .ToDictionary(g => g.Key, g => g.First().Test.Name);

    foreach (var pile in photos.GroupBy(p => p.TestId).OrderBy(g => testNames.GetValueOrDefault(g.Key, g.Key.ToString())))
    {
        var before = pile
            .Where(p => p.PhotoKind == (int)EnMedicalCasePhotoKind.Initial)
            .OrderBy(p => p.Slot)
            .Select(ToPhotoDataUri)
            .Where(uri => !string.IsNullOrWhiteSpace(uri))
            .Cast<string>()
            .ToList();

        var after = pile
            .Where(p => p.PhotoKind == (int)EnMedicalCasePhotoKind.Final)
            .OrderBy(p => p.Slot)
            .Select(ToPhotoDataUri)
            .Where(uri => !string.IsNullOrWhiteSpace(uri))
            .Cast<string>()
            .ToList();

        if (before.Count == 0 || after.Count == 0)
            continue;

        var testName = testNames.GetValueOrDefault(pile.Key, _loc.GetSystem("Test"));

        sb.Append($@"
        <div class='photo-compare-page'>
            <h2>{_loc.GetSystem("TestPhoto_CompareTitle")}: {testName}</h2>
            <table class='photo-grid'>
                <tr><td class='photo-row-label' colspan='2'>{_loc.GetSystem("TestPhoto_Before")}</td></tr>
                <tr>
                    {PhotoCellHtml(before.ElementAtOrDefault(0))}
                    {PhotoCellHtml(before.ElementAtOrDefault(1))}
                </tr>
                <tr><td class='photo-row-label' colspan='2'>{_loc.GetSystem("TestPhoto_After")}</td></tr>
                <tr>
                    {PhotoCellHtml(after.ElementAtOrDefault(0))}
                    {PhotoCellHtml(after.ElementAtOrDefault(1))}
                </tr>
            </table>
        </div>");
    }
}

private static string PhotoCellHtml(string? dataUri)
{
    if (string.IsNullOrWhiteSpace(dataUri))
        return "<td class='photo-cell'></td>";

    return $"<td class='photo-cell'><img src='{dataUri}' alt='' /></td>";
}

private string? ToPhotoDataUri(MedicalCaseTestPhoto photo)
{
    if (string.IsNullOrWhiteSpace(photo.FileName))
        return null;

    var safeFileName = Path.GetFileName(photo.FileName);
    var path = Path.Combine(_photosPath, safeFileName);
    if (!File.Exists(path))
        return null;

    var bytes = File.ReadAllBytes(path);
    if (bytes.Length == 0)
        return null;

    var mime = Path.GetExtension(safeFileName).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };

    return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
}

        public async Task<MedicalReportPdfResult> GeneratePdfAsync(
            MedicalCase medicalCase,
            string lang,
            HttpUser httpUser,
            bool saveToHistory)
        {
            var html = await GenerateHtmlReport(medicalCase, lang);
            var pdfBytes = _pdfPrintService.GeneratePdf(httpUser, html, $"Medical Report - {medicalCase.Name}");

            if (saveToHistory && medicalCase.Patient != null && !string.IsNullOrWhiteSpace(medicalCase.Patient.UserId))
            {
                Directory.CreateDirectory(_reportsPath);

                var storedFileName = $"{Guid.NewGuid()}_{medicalCase.Name}.pdf";
                await File.WriteAllBytesAsync(Path.Combine(_reportsPath, storedFileName), pdfBytes);

                _context.MedicalReportHistories.Add(new MedicalReportHistory
                {
                    MedicalCaseId = medicalCase.Id,
                    UserId = medicalCase.Patient.UserId,
                    ReportUrl = storedFileName,
                    CreatedAt = DateTime.UtcNow,
                });
                await _context.SaveChangesAsync();
            }

            var filePrefix = IsGerman(lang) ? "MedizinischerBericht" : "MedicalReport";
            var lastName = medicalCase.Patient?.User?.LastName ?? "Patient";

            return new MedicalReportPdfResult
            {
                PdfBytes = pdfBytes,
                DownloadFileName = $"{filePrefix}_Case{medicalCase.Id}_{lastName}.pdf"
            };
        }

        public FileStream? OpenReportFile(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            var safeFileName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName != fileName)
                return null;

            var path = Path.Combine(_reportsPath, safeFileName);
            var fullPath = Path.GetFullPath(path);
            var fullFolder = Path.GetFullPath(_reportsPath);

            if (!fullPath.StartsWith(fullFolder, StringComparison.OrdinalIgnoreCase))
                return null;

            if (!File.Exists(fullPath))
                return null;

            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

    }
}