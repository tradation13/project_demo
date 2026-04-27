using System.Text;
using System.Text.Json;
using IPTS.Models.Entites;
using IPTS.Resources;
using System.Globalization; // تأكد من إضافة هذا في الأعلى

namespace IPTS.Services
{
    public class MedicalReportService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        private readonly LocService _loc;

        public MedicalReportService(LocService loc,HttpClient httpClient, IConfiguration configuration)
        {
            _loc = loc;
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"];
        }
public async Task<string> GenerateHtmlReport(MedicalCase medicalCase, string lang = "en")
{
    // 1. طلب تحليل الذكاء الاصطناعي أولاً
    string aiAnalysis = await GetAiAnalysisAsync(medicalCase, lang);

string generalAnalysis = ExtractGeneralAnalysis(aiAnalysis);

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

    /* Info Sections */
    .info-section {{
        display: flex;
        flex-wrap: wrap;
        justify-content: space-between;
        gap: 20px;
        margin-bottom: 30px;
    }}

    .info-box {{
        flex: 1 1 45%;
        background-color: #f0f4f8;
        border: 1px solid #cfd8dc;
        border-radius: 8px;
        padding: 15px;
        box-shadow: 0 2px 4px rgba(0,0,0,0.05);
        transition: all 0.3s ease;
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

    /* Footer */
    .footer {{
        border-top: 2px solid #004d40;
        margin-top: 50px;
        padding-top: 10px;
        font-size: 0.8em;
        color: #777;
        text-align: right;
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

        {(medicalCase.Height.HasValue || medicalCase.Weight.HasValue || medicalCase.BloodGroup != null ? $@"
        <div class='info-box'>
            <h3>{_loc.GetSystem("PhysicalMeasurements")}</h3>
            {(medicalCase.Height.HasValue ? $"<p><strong>{_loc.GetSystem("Height")}:</strong> {medicalCase.Height} cm</p>" : "")}
            {(medicalCase.Weight.HasValue ? $"<p><strong>{_loc.GetSystem("Weight")}:</strong> {medicalCase.Weight} kg</p>" : "")}
            {(medicalCase.BloodGroup != null ? $"<p><strong>{_loc.GetSystem("BloodGroup")}:</strong> {medicalCase.BloodGroup}</p>" : "")}
        </div>" : "")}

        {(medicalCase.IsSmoker.HasValue || medicalCase.HasChronicDisease.HasValue || medicalCase.ActivityLevel != null ? $@"
        <div class='info-box'>
            <h3>{_loc.GetSystem("HealthHistory")}</h3>
            {(medicalCase.IsSmoker.HasValue ? $"<p><strong>{_loc.GetSystem("IsSmoker")}:</strong> {(medicalCase.IsSmoker == true ? _loc.GetSystem("Yes") : _loc.GetSystem("No"))}</p>" : "")}
            {(medicalCase.HasChronicDisease.HasValue ? $"<p><strong>{_loc.GetSystem("ChronicDiseases")}:</strong> {(medicalCase.HasChronicDisease == true ? _loc.GetSystem("Yes") : _loc.GetSystem("No"))}</p>" : "")}
            {(medicalCase.ActivityLevel != null ? $"<p><strong>{_loc.GetSystem("ActivityLevel")}:</strong> {medicalCase.ActivityLevel}</p>" : "")}
        </div>" : "")}
    </div>

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
    AppendTestAnalysisBox(sb, testAnalyses[testName]);
}
                        var orderedTests = test.OrderBy(t => t.CreatedAt).ToList();
                        
                        // استدعاء ميثود الرسم البياني والجدول
                        AppendTestVisuals(sb, orderedTests);
                    }
                    sb.Append("</div>");
                }
            }

    // الفوتر مع ترجمة "Generated on"
    sb.Append($@"<div class='footer'>{_loc.GetSystem("GeneratedOn")} {DateTime.Now:yyyy-MM-dd HH:mm}</div></div></body></html>");

    return sb.ToString();
}
private async Task<string> GetAiAnalysisAsync(MedicalCase medicalCase, string lang = "en")
{
    try
    {
        Console.WriteLine("\n--- [HUGGING FACE ROUTER REQUEST START] ---");
        
        // 1. تحديد اسم اللغة بالكامل لإرشاد الموديل بدقة
        string targetLanguageName = lang.ToLower() == "de" ? "German" : "English";
        // 1. الرابط الجديد من الـ curl
        var url = "https://api.openai.com/v1/chat/completions";
        
  // 1. بناء بيانات المريض البدنية والتاريخ الصحي ديناميكياً (فقط للقيم الموجودة)
var vitalsSb = new StringBuilder();
if (medicalCase.Height.HasValue) vitalsSb.AppendLine($"- Height: {medicalCase.Height} cm");
if (medicalCase.Weight.HasValue) vitalsSb.AppendLine($"- Weight: {medicalCase.Weight} kg");
if (medicalCase.BloodGroup != null) vitalsSb.AppendLine($"- Blood Group: {medicalCase.BloodGroup}");
if (medicalCase.IsSmoker.HasValue) vitalsSb.AppendLine($"- Is Smoker: {(medicalCase.IsSmoker == true ? "Yes" : "No")}");
if (medicalCase.HasChronicDisease.HasValue) vitalsSb.AppendLine($"- Has Chronic Diseases: {(medicalCase.HasChronicDisease == true ? "Yes" : "No")}");
if (medicalCase.ActivityLevel != null) vitalsSb.AppendLine($"- Activity Level: {medicalCase.ActivityLevel}");
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

Normal Reference:
<describe normal values for age and sex when possible>

Clinical Interpretation:
<short explanation>

Progress Evaluation:
<trend explanation>

Physiotherapy Focus:
<specific therapy advice>

TEST_ANALYSIS_END";

// 3. بناء الرسالة (User Content) مع دمج البيانات الحيوية والنتائج المخبرية
var userContent = $@"
Please analyze the following medical case and provide the response in {targetLanguageName}:
- Patient Condition: {medicalCase.Description}

{(vitalsSb.Length > 0 ? "- Patient Physical Info & History:\n" + vitalsSb.ToString() : "")}

- Clinical Test Results (Progress Metrics): 
  {string.Join(", ", medicalCase.MedicalCaseTests.Select(t => $"{t.Test?.Name}: {t.Result}"))}

IMPORTANT INSTRUCTIONS:
- The 'Result' values are progress measurements (degrees/percentages). 
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



Please provide the response using this structure:

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

Normal Reference:
<exact numeric range with units, must be present>

TEST_ANALYSIS_END

Important rules:
- Do NOT repeat test analysis in the general section.
- Do NOT use Markdown symbols (#, **, -, etc).
- Only plain structured text.";
       
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
    return aiText?.Trim() ?? "No analysis content returned.";
}
        
        Console.WriteLine($"API Error: {response.StatusCode} - {responseBody}");
        return "Analysis is temporarily unavailable (AI Provider Busy).";
    }
    catch (Exception ex) 
    {
        Console.WriteLine($"Critical Error: {ex.Message}");
        return "Connection to AI service failed.";
    }
}

private void AppendTestVisuals(StringBuilder sb, List<MedicalCaseTest> tests)
{
    if (tests == null || !tests.Any()) return;

    var inv = CultureInfo.InvariantCulture;
    var values = tests.Select(t => double.TryParse(t.Result, out var r) ? r : 0.0).ToList();
    
    double maxVal = values.Max();
    double minVal = values.Min();
    double avgVal = values.Average();

    // 1. بطاقات الإحصائيات العلوية
    sb.Append(@"<div style='display: grid; grid-template-columns: repeat(auto-fit, minmax(120px, 1fr)); gap: 15px; margin-bottom: 25px; font-family: sans-serif;'>");
    AppendStatCard(sb, _loc.GetSystem("Highest"), maxVal.ToString("F1", inv), "#e8f5e9", "#2e7d32");
    AppendStatCard(sb, _loc.GetSystem("Lowest"), minVal.ToString("F1", inv), "#ffebee", "#c62828");
    AppendStatCard(sb, _loc.GetSystem("Average"), avgVal.ToString("F1", inv), "#e3f2fd", "#1565c0");
    sb.Append("</div>");

    // 2. قسم مدى التحسن (New: Improvement Summary)
    if (tests.Count > 1)
    {
        double firstVal = values.First();
        double lastVal = values.Last();
        double diff = lastVal - firstVal;
        double percent = (firstVal != 0) ? (diff / firstVal) * 100 : 0;
        
        string statusText = diff > 0 ? "Increased" : (diff < 0 ? "Decreased" : "Stable");
        string statusColor = diff > 0 ? "#2e7d32" : (diff < 0 ? "#c62828" : "#1565c0");
        string arrow = diff > 0 ? "↑" : (diff < 0 ? "↓" : "↔");

        sb.Append($@"
            <div style='background:#fcfcfc; border:1px solid #eee; padding:15px; border-radius:12px; margin-bottom:25px; font-family:sans-serif; display:flex; align-items:center; justify-content:space-between;'>
                <div>
                    <div style='font-size:12px; color:#7f8c8d; font-weight:bold; text-transform:uppercase;'>Overall Progress</div>
                    <div style='font-size:18px; font-weight:bold; color:#2c3e50; margin-top:5px;'>
                        {statusText} by <span style='color:{statusColor};'>{Math.Abs(percent).ToString("F1", inv)}%</span> {arrow}
                    </div>
                </div>
                <div style='text-align:right;'>
                    <div style='font-size:11px; color:#95a5a6;'>Since first test ({tests.First().CreatedAt:yyyy-MM-dd})</div>
                </div>
            </div>");

        // 3. إعدادات الرسم البياني (SVG)
        var dates = tests.Select(t => t.CreatedAt.ToString("MM-dd")).ToList();
        double width = 600; 
        double height = 250;
        double padding = 50;
        double range = (maxVal - minVal) == 0 ? 1 : (maxVal - minVal);

        var pointsList = new List<string>();
        for (int i = 0; i < values.Count; i++)
        {
            double x = padding + i * (width - 2 * padding) / (values.Count - 1);
            double y = height - padding - ((values[i] - minVal) / range) * (height - 2 * padding);
            pointsList.Add($"{x.ToString(inv)},{y.ToString(inv)}");
        }

        sb.Append($@"
            <div style='text-align:center; margin-top:10px; padding:15px; background:#fff; border:1px solid #eee; border-radius:12px; shadow: 0 4px 15px rgba(0,0,0,0.03);'>
                <svg width='100%' height='{height}' viewBox='0 0 {width} {height}' style='font-family:Arial, sans-serif; overflow:visible;'>
                    <line x1='{padding.ToString(inv)}' y1='{padding.ToString(inv)}' x2='{(width - padding).ToString(inv)}' y2='{padding.ToString(inv)}' stroke='#f5f5f5' stroke-dasharray='5,5'/>
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
                    </tr>");
        foreach (var t in tests) 
        {
            sb.Append($@"<tr style='border-bottom:1px solid #f9f9f9;'>
                         <td style='padding:10px; color:#666;'>{t.CreatedAt:yyyy-MM-dd}</td>
                         <td style='padding:10px; text-align:center; font-weight:bold; color:#00695c;'>{t.Result}</td></tr>");
        }
        sb.Append("</table></div>");
    }
    else
    {
        var single = tests.First();
        sb.Append($@"<div style='padding:25px; background:#e0f2f1; border-radius:12px; text-align:center; margin-top:20px;'>
                        <div style='color:#00796b; font-size:1.1em;'>{_loc.GetSystem("LatestResult")}</div>
                        <div style='font-size:2.5em; font-weight:bold; color:#004d40;'>{single.Result}</div>
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


private void AppendTestAnalysisBox(StringBuilder sb, string analysis)
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
            Clinical AI Analysis
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

    }
}