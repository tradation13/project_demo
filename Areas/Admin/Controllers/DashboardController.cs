using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IPTS.Services;
using IPTS.Data;
using Microsoft.EntityFrameworkCore;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Helpers;
using Serilog.Events;

namespace IPTS.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = "admin")]
    public class DashboardController : Controller
    {
        private readonly PatientService _patientService;
        private readonly UserService _userService;
        private readonly MedicalCaseService _medicalCaseService;
        private readonly TestService _testService;
        private readonly TestGroupService _testGroupService;
        private readonly ChatbotService _chatbotService;
        private readonly ApplicationDbContext _context;

        public DashboardController(
            PatientService patientService,
            UserService userService,
            MedicalCaseService medicalCaseService,
            TestService testService,
            TestGroupService testGroupService,
            ChatbotService chatbotService,
            ApplicationDbContext context)
        {
            _patientService = patientService;
            _userService = userService;
            _medicalCaseService = medicalCaseService;
            _testService = testService;
            _testGroupService = testGroupService;
            _chatbotService = chatbotService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var statistics = new DashboardStatisticsViewModel
            {
                TotalPatients = await _patientService.CountAsync(),
                TotalDoctors = await _context.Doctors.AsNoTracking().CountAsync(),
                TotalAdmins = await _context.Admins.AsNoTracking().CountAsync(),
                TotalMedicalCases = await _medicalCaseService.CountAsync(),
                TotalTests = await _testService.CountAsync(),
                TotalTestGroups = await _testGroupService.CountAsync(),
                TotalUserTypes = await _context.UserTypes.AsNoTracking().CountAsync(),
                TotalChatConversations = await _chatbotService.GetConversationsWithMessagesCountAsync()
            };

            LogHelper.LogWithContext(
                "Loaded admin dashboard statistics",
                User?.Identity?.Name ?? "Unknown",
                "Admin",
                "DashboardController.Index",
                LogEventLevel.Information);

            return View(statistics);
        }
    }
}
