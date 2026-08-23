using IPTS.Areas.Doctor.ViewsModels;
using IPTS.Data;
using IPTS.Helpers;
using IPTS.Models.Entites;
using IPTS.Resources;
using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;
using System.Security.Claims;

namespace IPTS.Areas.Doctor.Controllers
{
    [Area("doctor")]
    [Authorize(Roles = "doctor")]
    public class DashboardController(
        LocService locService,
        ApplicationDbContext context,
        UserService userService,
        MedicalCaseService medicalCaseService,
        MedicalCaseTestService medicalCaseTestService) : Controller
    {
        private readonly LocService _locService = locService;
        private readonly ApplicationDbContext _context = context;
        private readonly UserService _userService = userService;
        private readonly MedicalCaseService _medicalCaseService = medicalCaseService;
        private readonly MedicalCaseTestService _medicalCaseTestService = medicalCaseTestService;

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var doctor = (await _userService.GetByIdAsync(userId, q => q.Include(u => u.Doctor))).Doctor;
            if (doctor == null) return NotFound(_locService.GetSystem("Error_DoctorProfileNotFound"));

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var sevenDaysAgo = now.Date.AddDays(-6);
            var doctorId = doctor.Id;

            var appointments = _context.Appointments.AsNoTracking().Where(a => a.DoctorId == doctorId);

            var kpi = await appointments
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Upcoming = g.Count(a => a.ScheduledTime >= now
                        && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)),
                    MonthTotal = g.Count(a => a.ScheduledTime >= startOfMonth),
                    CompletedThisMonth = g.Count(a => a.ScheduledTime >= startOfMonth && a.ScheduledTime < now),
                    CancelledThisMonth = g.Count(a => a.ScheduledTime >= startOfMonth && a.Status == AppointmentStatus.Cancelled),
                })
                .FirstOrDefaultAsync();

            var totalAppointments = kpi?.Total ?? 0;
            var upcomingAppointments = kpi?.Upcoming ?? 0;
            var monthCompleted = kpi?.CompletedThisMonth ?? 0;
            var cancelledThisMonth = kpi?.CancelledThisMonth ?? 0;
            var uniquePatients = await appointments.Select(a => a.PatientId).Distinct().CountAsync();
            var monthTotal = kpi?.MonthTotal ?? 0;

            var nextAppointmentUtc = await appointments
                .Where(a => a.Status == AppointmentStatus.Confirmed && a.ScheduledTime > now)
                .OrderBy(a => a.ScheduledTime)
                .Select(a => (DateTime?)a.ScheduledTime)
                .FirstOrDefaultAsync();

            var last7DaysCounts = await appointments
                .Where(a => a.ScheduledTime >= sevenDaysAgo && a.ScheduledTime <= now)
                .GroupBy(a => a.ScheduledTime.Date)
                .Select(g => new { Day = g.Key, Cnt = g.Count() })
                .ToListAsync();

            var pendingEntities = await appointments
                .Include(a => a.Patient).ThenInclude(p => p!.User)
                .Include(a => a.Doctor).ThenInclude(d => d!.User)
                .Where(a => a.Status == AppointmentStatus.Pending)
                .OrderByDescending(a => a.ScheduledTime)
                .Take(3)
                .ToListAsync();

            var activeMedicalCases = await _medicalCaseService.CountAsync(a => a.DoctorId == doctorId);
            var testsThisMonth = await _medicalCaseTestService.CountAsync(q =>
                q.Where(t => t.MedicalCase.DoctorId == doctorId && t.CreatedAt >= startOfMonth));

            var completionRate = monthTotal == 0 ? 0.0 : (monthCompleted * 100.0 / monthTotal);

            var dailyMap = last7DaysCounts.ToDictionary(x => x.Day, x => x.Cnt);
            int sum7 = 0;
            for (var d = 0; d < 7; d++)
            {
                var day = sevenDaysAgo.AddDays(d);
                dailyMap.TryGetValue(day, out int count);
                sum7 += count;
            }

            var vm = new DoctorDashboardStatsViewModel
            {
                TotalAppointments = totalAppointments,
                UpcomingAppointments = upcomingAppointments,
                CompletedThisMonth = monthCompleted,
                CancelledThisMonth = cancelledThisMonth,
                UniquePatientsAllTime = uniquePatients,
                ActiveMedicalCases = activeMedicalCases,
                TestsOrderedThisMonth = testsThisMonth,
                CompletionRateThisMonthPercent = Math.Round(completionRate, 1),
                AvgAppointmentsPerDayLast7Days = Math.Round(sum7 / 7.0, 2),
                NextAppointmentUtc = nextAppointmentUtc,
                LatestPendingRequests = pendingEntities.Select(a => new AppointmentViewModel
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient?.User == null
                        ? string.Empty
                        : $"{a.Patient.User.FirstName} {a.Patient.User.LastName}".Trim(),
                    PatientPhone = a.Patient?.User?.PhoneNumber ?? string.Empty,
                    PatientEmail = a.Patient?.User?.Email ?? string.Empty,
                    DoctorName = a.Doctor?.User == null
                        ? string.Empty
                        : $"{a.Doctor.User.FirstName} {a.Doctor.User.LastName}".Trim(),
                    DoctorId = a.DoctorId,
                    ScheduledTime = a.ScheduledTime,
                    Status = a.Status,
                    Notes = a.Notes,
                    PrescriptionFileName = a.PrescriptionFileName,
                    StartSlotIndex = a.StartSlotIndex,
                    EndSlotIndex = a.EndSlotIndex
                }).ToList()
            };

            LogHelper.LogWithContext(
                "Loaded doctor dashboard statistics",
                User?.Identity?.Name ?? "Unknown",
                "Doctor",
                "DashboardController.Index",
                LogEventLevel.Information);

            return View(vm);
        }
    }
}
