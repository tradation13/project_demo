using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IPTS.Services;
using IPTS.Data;
using Microsoft.EntityFrameworkCore;
using IPTS.Areas.Admin.ViewsModels;

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
        private readonly ApplicationDbContext _context;

        public DashboardController(
            PatientService patientService,
            UserService userService,
            MedicalCaseService medicalCaseService,
            TestService testService,
            TestGroupService testGroupService,
            ApplicationDbContext context)
        {
            _patientService = patientService;
            _userService = userService;
            _medicalCaseService = medicalCaseService;
            _testService = testService;
            _testGroupService = testGroupService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var statistics = new DashboardStatisticsViewModel
            {
                TotalPatients = (await _patientService.GetAllAsync()).Count,
                TotalDoctors = (await _context.Doctors.CountAsync()),
                TotalAdmins = (await _context.Admins.CountAsync()),
                TotalMedicalCases = (await _medicalCaseService.GetAllAsync()).Count,
                TotalTests = (await _testService.GetAllAsync()).Count,
                TotalTestGroups = (await _testGroupService.GetAllAsync()).Count,
                TotalUserTypes = (await _context.UserTypes.CountAsync())
            };

            return View(statistics);
        }
    }
}
