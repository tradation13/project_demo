using IPTS.Areas.Doctor.ViewsModels;
using IPTS.Data;
using IPTS.Models.Entites;
using IPTS.Resources;
using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IPTS.Areas.Doctor.Controllers
{
    [Area("doctor")]
    [Authorize(Roles = "doctor")]

    public class PatientsController(LocService locService,ApplicationDbContext context, UserService userService, AppointmentService appointmentService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserService _userService = userService;
        private readonly AppointmentService _appointmentService = appointmentService;
        private readonly LocService _locService = locService;



[HttpGet]
public IActionResult Create()
{
    return View();
}


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(PatientRegistrationViewModel model)
{
    
    if (!ModelState.IsValid) 
    {
        return View(model); 
    }

    try 
    {
        
        await _userService.RegisterPatientFromDoctorAsync(model);

        
    TempData["SuccessMessage"] = string.Format(
    _locService.GetSystem("Msg_PatientRegisteredPasswordInfo"), 
    model.NationalId
);
        
        return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
       
        ModelState.AddModelError(string.Empty, ex.Message);
        return View(model);
    }
}

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return NotFound();

            // Explicitly specify the type argument for GetAllAsync to resolve CS0411  
            var patientIds = (await _appointmentService.GetAllAsync(q => q.Where(a => a.DoctorId == doctor.Id))).Select(a => a.PatientId).Distinct();

            var patients = await _context.Patients
                .Include(p => p.User)
                .Where(p => patientIds.Contains(p.Id))
                .ToListAsync();

            return View(patients);
        }
            public async Task<IActionResult> PreviousAppointments(int patientId)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                if (doctor == null) return NotFound();

                var appointments = await _context.Appointments
                    .Include(a => a.Patient).ThenInclude(p => p.User)
                    .Where(a => a.DoctorId == doctor.Id && a.PatientId == patientId)
                    .OrderByDescending(a => a.ScheduledTime)
                    .ToListAsync();

                return View(appointments);
            }
    }

    
}
