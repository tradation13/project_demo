using IPTS.Areas.Patient.ViewsModels;
using IPTS.Helpers;
using IPTS.Resources;
using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Math.EC;
using System.Security.Claims;

namespace IPTS.Models.Entites
{
    [Area("patient")]
    [Authorize(Roles = "patient")]
    public class PanelController(LocService locService,UserService userService, AppointmentService appointmentService, MedicalCaseService medicalCaseService) : Controller
    {
        private readonly LocService _locService = locService;
        private readonly UserService _userService = userService;
        private readonly AppointmentService _appointmentService = appointmentService;
        private readonly MedicalCaseService _medicalCaseService = medicalCaseService;
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Get the patient entity for the current user
            var patient = await _userService.GetByIdAsync(userId, q => q.Include(u => u.Patient));
            if (patient?.Patient == null)
                return NotFound(_locService.GetSystem("Error_PatientProfileNotFound"));

            var appointmentsCount = await _appointmentService.CountAsync(a => a.PatientId == patient.Patient.Id);
            var medicalCasesCount = await _medicalCaseService.CountAsync(a => a.PatientId == patient.Patient.Id);

            var vm = new PatientPanelViewModel
            {
                AppointmentsCount = appointmentsCount,
                MedicalCasesCount = medicalCasesCount
            };

            LogHelper.LogWithContext(
                "Loaded patient panel counts",
                userId,
                "patient",
                "PanelController.Index",
                Serilog.Events.LogEventLevel.Information);

            return View(vm);
        }
      
    }
}
