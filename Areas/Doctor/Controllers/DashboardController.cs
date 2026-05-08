using AutoMapper;
using IPTS.Areas.Doctor.ViewsModels;
using IPTS.Data;
using IPTS.Models.Entites;
using IPTS.Resources;
using IPTS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IPTS.Areas.Doctor.Controllers
{
    [Area("doctor")]
    [Authorize(Roles = "doctor")]
    public class DashboardController(LocService locService,AppointmentService appointmentService, IMapper mapper, UserManager<AppUser> userManager, ApplicationDbContext context, UserService userService, MedicalCaseService medicalCaseService, MedicalCaseTestService medicalCaseTestService) : Controller
    {
        private readonly LocService _locService = locService;
        private readonly AppointmentService _appointmentService = appointmentService;
        private readonly IMapper _mapper = mapper;
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly ApplicationDbContext _context = context;
        private readonly UserService _userService = userService;
        private readonly MedicalCaseService _medicalCaseService = medicalCaseService;
        private readonly MedicalCaseTestService _medicalCaseTestService = medicalCaseTestService;
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Resolve current doctor
            var doctor = (await _userService.GetByIdAsync(userId, q => q.Include(u => u.Doctor))).Doctor;

            if (doctor == null) return NotFound(_locService.GetSystem("Error_DoctorProfileNotFound"));

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var sevenDaysAgo = now.Date.AddDays(-6); 
            
            var apptsQ = await _appointmentService.GetAllAsync(q => q.Where(a => a.DoctorId == doctor.Id));

           
            var totalAppointmentsTask = apptsQ.Count;
            var upcomingAppointmentsTask = apptsQ.Count(a => a.ScheduledTime >= now
                && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed));
            var completedThisMonthTask = apptsQ.Count(a =>
                a.ScheduledTime >= startOfMonth &&
                a.ScheduledTime < DateTime.UtcNow);
            var cancelledThisMonthTask = apptsQ.Count(a =>
                a.ScheduledTime >= startOfMonth &&
                (a.Status == AppointmentStatus.Cancelled) 
            );

            
            var uniquePatientsAllTimeTask = apptsQ.Select(a => a.PatientId).Distinct().Count();

            
            var activeMedicalCasesTask = (await _medicalCaseService.GetAllAsync(q => q.Where(a => a.DoctorId == doctor.Id))).Count;

            
            var testsThisMonthTask = (await _medicalCaseTestService.GetAllAsync(q => q.Include(mt => mt.MedicalCase).Where(a => a.MedicalCase.DoctorId == doctor.Id))).Count;

           
            var monthTotalTask = apptsQ.Count(a => a.ScheduledTime >= startOfMonth);
          
            var nextAppointmentTask = apptsQ
                .Where(a => a.ScheduledTime >= now &&
                            (a.Status == AppointmentStatus.Confirmed))
                .OrderBy(a => a.ScheduledTime)
                .Select(a => a.ScheduledTime)
                .FirstOrDefault() > DateTime.UtcNow;

           
            var last7daysCountsTask = apptsQ
                .Where(a => a.ScheduledTime >= sevenDaysAgo && a.ScheduledTime <= now)
                .GroupBy(a => a.ScheduledTime.Date)
                .Select(g => new { Day = g.Key, Cnt = g.Count() })
                .ToList();

      

            var monthCompleted = completedThisMonthTask;
            var monthTotal = monthTotalTask;
            var completionRate = monthTotal == 0 ? 0.0 : (monthCompleted * 100.0 / monthTotal);

            
            var dailyMap = last7daysCountsTask.ToDictionary(x => x.Day, x => x.Cnt);
            int sum7 = 0;
            for (var d = 0; d < 7; d++)
            {
                var day = sevenDaysAgo.AddDays(d);
                dailyMap.TryGetValue(day, out int count);
                sum7 += count;
            }
            var avgPerDay = sum7 / 7.0;

            var vm = new DoctorDashboardStatsViewModel
            {
                TotalAppointments = totalAppointmentsTask,
                UpcomingAppointments = upcomingAppointmentsTask,
                CompletedThisMonth = monthCompleted,
                CancelledThisMonth = cancelledThisMonthTask,
                UniquePatientsAllTime = uniquePatientsAllTimeTask,
                ActiveMedicalCases = activeMedicalCasesTask,
                TestsOrderedThisMonth = testsThisMonthTask,
                CompletionRateThisMonthPercent = Math.Round(completionRate, 1),
                AvgAppointmentsPerDayLast7Days = Math.Round(avgPerDay, 2),
                NextAppointmentUtc = null
            };


            return View(vm); 
        }
    }
}
