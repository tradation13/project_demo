using IPTS.Areas.Doctor.ViewsModels;
using IPTS.Data;
using IPTS.Models.Entites;
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

    public class PatientsController(ApplicationDbContext context, UserService userService, AppointmentService appointmentService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly UserService _userService = userService;
        private readonly AppointmentService _appointmentService = appointmentService;


// 1. GET: لعرض صفحة إضافة مريض جديد
[HttpGet]
public IActionResult Create()
{
    return View();
}

// 2. POST: لاستقبال البيانات من الفورم وحفظها في قاعدة البيانات
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(PatientRegistrationViewModel model)
{
    // 1. التحقق من صحة المدخلات (Validation Attributes) التي وضعناها في الـ ViewModel
    if (!ModelState.IsValid) 
    {
        return View(model); 
    }

    try 
    {
        // 2. استدعاء الخدمة لتنفيذ عملية التسجيل المعقدة
        await _userService.RegisterPatientFromDoctorAsync(model);

        // 3. إذا تمت العملية بنجاح، نرسل رسالة نجاح ونوجه الدكتور لصفحة القائمة
        TempData["Success"] = $"Patient registered! Password format is: Aa{model.NationalId}_1";
        return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
        // 4. في حال حدوث خطأ (مثل: الإيميل مكرر)، نعرض الرسالة القادمة من الـ Service في الـ View
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
