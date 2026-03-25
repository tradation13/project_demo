using AutoMapper;
using IPTS.Data;
using IPTS.Models.Entites;
using IPTS.Services;
using IPTS.ViewModels;
using IPTS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;
using IPTS.Helpers;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using IPTS.Areas.Doctor.ViewsModels;
using IPTS.Resources;
namespace IPTS.Areas.Doctor.Controllers
{
    [Area("doctor")]
    [Authorize(Roles = "doctor")]
    public class AppointmentsController(LocService locService,AppointmentService appointmentService, IMapper mapper, UserManager<AppUser> userManager, ApplicationDbContext context, UserService userService, IdentityErrorTranslator identityErrorTranslator, IFileService fileService) : Controller
    {
        private readonly AppointmentService _appointmentService = appointmentService;
        private readonly LocService _locService = locService;
        private readonly IMapper _mapper = mapper;
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly ApplicationDbContext _context = context;
        private readonly UserService _userService = userService;
        private readonly IdentityErrorTranslator _identityErrorTranslator = identityErrorTranslator;
        private readonly IFileService _fileService = fileService;

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var appointments = await _appointmentService.GetAppointmentsForDoctorAsync(userId);

                // Log important action  
                LogHelper.LogWithContext(
                    "Viewed appointments list",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.Index",
                    LogEventLevel.Information
                );

                return View(appointments);
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error loading appointments list: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.Index",
                    LogEventLevel.Fatal
                );
                throw;
            }
        }



        [HttpGet]
        public IActionResult SearchPatient()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error opening patient search form: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.SearchPatient",
                    LogEventLevel.Fatal
                );
                throw;
            }
        }

        [HttpPost]
public async Task<IActionResult> SearchPatient([FromForm] string? SearchName, [FromForm] string? PhoneNumber, [FromForm] string? Email)
{
    try
    {
        // 1. التأكد من إدخال معلومة واحدة على الأقل
        if (string.IsNullOrWhiteSpace(SearchName) &&
            string.IsNullOrWhiteSpace(PhoneNumber) &&
            string.IsNullOrWhiteSpace(Email))
        {
            TempData["ErrorMessage"] = _locService.GetSystem("Msg_SearchCriteriaRequired");
            return RedirectToAction("SearchPatient");
        }

        IPTS.Models.Entites.Patient? patient = null;

        // 2. البحث الذكي (استخدام السيرفيس الجديدة)
        // نبحث أولاً بالاسم إذا تم اختياره من القائمة أو كتابته
        var patients = await _appointmentService.SearchPatientsAsync(SearchName ?? PhoneNumber ?? Email ?? "");
        
        // إذا وجدنا نتائج، نأخذ أول مريض (الأكثر مطابقة)
        patient = patients.FirstOrDefault();

        // 3. إذا لم نجد مريضاً بالمطابقة العامة، نجرب البحث المباشر بالهاتف أو الإيميل كخطة بديلة
        if (patient == null && !string.IsNullOrWhiteSpace(PhoneNumber))
        {
            patient = await _appointmentService.SearchPatientByPhoneAsync(PhoneNumber);
        }

        if (patient == null && !string.IsNullOrWhiteSpace(Email))
        {
            patient = await _appointmentService.SearchPatientByEmailAsync(Email);
        }

        // 4. في حال لم يتم العثور على أي مريض
        if (patient == null)
        {
            TempData["WarningMessage"] = _locService.GetSystem("Msg_NotFoundCreateNew");
            TempData["Draft_Name"] = SearchName;
            TempData["Draft_Phone"] = PhoneNumber;
            TempData["Draft_Email"] = Email;
            return RedirectToAction("CreatePatient");
        }

        // 5. تم العثور على المريض -> التوجه لجدولة الموعد
        // نضمن تحميل بيانات الـ User لتجنب الـ Null في الاسم
        var patientName = patient.User != null 
            ? $"{patient.User.FirstName} {patient.User.LastName}".Trim() 
            : _locService.GetSystem("Label_Unknown");

        TempData["PatientId"] = patient.Id;
        TempData["PatientName"] = patientName;
        TempData["SuccessMessage"] = $"{_locService.GetSystem("Status_PatientFound")}: {patientName}.";

        return RedirectToAction("ScheduleAppointment");
    }
    catch (Exception ex)
    {
        LogHelper.LogWithContext(
            $"Error searching for patient: {ex.Message}",
            User?.Identity?.Name ?? "Unknown",
            "Doctor",
            "AppointmentsController.SearchPatient",
            LogEventLevel.Fatal
        );
        TempData["ErrorMessage"] = _locService.GetSystem("Error_TechnicalSearch");
        return RedirectToAction("SearchPatient");
    }
}

        [HttpGet]
        public IActionResult CreatePatient()
        {
            try
            {
                var searchData = TempData["SearchData"];
                var model = new PatientCreateViewModel();
                
                if (searchData != null)
                {
                    // Pre-fill form with search data if available
                    // This will be handled in the view
                }
                
                TempData["InfoMessage"] = _locService.GetSystem("Msg_CreateAccountInfo");
                return View(model);
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error opening create patient form: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.CreatePatient",
                    LogEventLevel.Fatal
                );
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePatient(PatientCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["WarningMessage"] = _locService.GetSystem("Msg_ValidationError");
                    return View(model);
                }

                var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", _locService.GetSystem("Auth_EmailAlreadyExists"));
            TempData["WarningMessage"] = _locService.GetSystem("Msg_EmailInUse");
            return View(model);
        }

        var existingUserName = await _userManager.FindByNameAsync(model.UserName);
        if (existingUserName != null)
        {
            ModelState.AddModelError("UserName", _locService.GetSystem("Auth_UsernameAlreadyTaken"));
            return View(model);
        }

                // Create the patient using UserService
                var registerModel = new RegisterViewModel
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Password = model.Password,
                    ConfirmPassword = model.ConfirmPassword,
                    UserTypeName = "patient",
                    Patient = new PatientRegisterViewModel
                    {
                        // IdentityNumber = model.IdentityNumber,
                        BirthDate = model.BirthDate
                    }
                };

                // Use UserService to create the patient
                var result = await _userService.RegisterAsync(registerModel);
                
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = _locService.GetSystem("Msg_PatientCreatedScheduleInfo");
                    return RedirectToAction("SearchPatient");
                }
                else
                {
                    var translatedErrors = _identityErrorTranslator.TranslateErrorsList(result.Errors);
                    foreach (var error in translatedErrors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    TempData["WarningMessage"] = _locService.GetSystem("Msg_ValidationError");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error creating patient: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.CreatePatient",
                    LogEventLevel.Fatal
                );
                TempData["ErrorMessage"] = string.Format(_locService.GetSystem("Error_CreatePatient"), ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ScheduleAppointment()
        {
            try
            {
                var patientId = TempData["PatientId"] as int?;
                if (!patientId.HasValue)
                {
                    TempData["WarningMessage"] = _locService.GetSystem("Msg_NoPatientSelected");
                    return RedirectToAction("SearchPatient");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                
                if (doctor == null)
                {
                    TempData["ErrorMessage"] = _locService.GetSystem("Error_DoctorProfileNotFound");
                    return NotFound(_locService.GetSystem("Error_DoctorProfileNotFound"));
                }

                var model = new AppointmentCreateViewModel
                {
                    PatientId = patientId.Value,
                    DoctorId = doctor.Id,
                    ScheduledDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc)
                };

                TempData["InfoMessage"] = _locService.GetSystem("Msg_SelectAppointmentSlot");
                return View(model);
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error opening appointment scheduling form: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.ScheduleAppointment",
                    LogEventLevel.Fatal
                );
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleAppointment(AppointmentCreateViewModel model)
        {
            try
            {
               
                if (!ModelState.IsValid)
                {
                    TempData["WarningMessage"] = _locService.GetSystem("Msg_RequiredFieldsMissing");
                    return View(model);
                }

                // Validate time slot selection
                if (model.StartSlotIndex < 0 || model.EndSlotIndex < 0 || model.StartSlotIndex > model.EndSlotIndex)
                {
                    TempData["WarningMessage"] = _locService.GetSystem("Msg_InvalidTimeSlots");
                    return View(model);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                
                if (doctor == null)
                {
                    TempData["ErrorMessage"] = _locService.GetSystem("Error_DoctorProfileNotFound");
                    return NotFound(_locService.GetSystem("Error_DoctorProfileNotFound"));
                }

                model.DoctorId = doctor.Id;
                
                // Ensure the scheduled date is in UTC format
                if (model.ScheduledDate.Kind != DateTimeKind.Utc)
                {
                    model.ScheduledDate = DateTime.SpecifyKind(model.ScheduledDate, DateTimeKind.Utc);
                }

                // --- 2. الجزء المضاف: معالجة رفع الملف كما فعلنا عند المريض ---
        if (model.PrescriptionFile != null && model.PrescriptionFile.Length > 0)
        {
            // حفظ الملف باستخدام الـ FileService
            var fileName = await _fileService.SavePrescriptionFileAsync(model.PrescriptionFile);
            
            // تخزين اسم الملف في الـ ViewModel ليتم تمريره للسيرفيس
            model.PrescriptionFileName = fileName;
        }
        // -------------------------------------------------------
                
                // Calculate total slots and duration
                var totalSlots = model.TotalSlots;
                var totalDuration = model.TotalDurationMinutes;
                
                // Log the appointment data for debugging
                LogHelper.LogWithContext(
                    $"Creating appointment: Start={model.StartSlotIndex}, End={model.EndSlotIndex}, " +
                    $"Time={model.StartTime}-{model.EndTime}, Duration={totalDuration}min, Patient={model.PatientId}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.ScheduleAppointment",
                    LogEventLevel.Information
                );
                
                // Create a list of slot indices for the service
                var slotIndices = Enumerable.Range(model.StartSlotIndex, totalSlots).ToList();
                
                var result = await _appointmentService.CreateAppointmentWithSlotsAsync(model, slotIndices);

                if (result)
                {
                    TempData["SuccessMessage"] = string.Format(
    _locService.GetSystem("Msg_AppointmentCreateSuccessDetails"), 
    totalDuration, 
    model.StartTime, 
    model.EndTime
);
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = _locService.GetSystem("Error_AppointmentCreateFailed");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error creating appointment: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.ScheduleAppointment",
                    LogEventLevel.Fatal
                );
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTimeSlots(DateTime date)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                
                if (doctor == null)
                {
                    return Json(new { 
    success = false, 
    message = _locService.GetSystem("Error_DoctorProfileNotFound") 
});
                }

                // Ensure the date is in UTC format for PostgreSQL
                var utcDate = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                var timeSlots = await _appointmentService.GetAvailableTimeSlotsAsync(utcDate, doctor.Id);
                return Json(new { success = true, timeSlots });
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error getting time slots: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.GetTimeSlots",
                    LogEventLevel.Fatal
                );
                return Json(new { 
    success = false, 
    message = _locService.GetSystem("Error_LoadingTimeSlots") 
});
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                // Verify that the appointment belongs to the current doctor
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                
                if (doctor == null)
                {
                    return NotFound(_locService.GetSystem("Error_DoctorProfileNotFound"));
                }

                var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
                if (appointment == null) return NotFound();

                LogHelper.LogWithContext(
                    $"Viewed appointment details {id}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.Details",
                    LogEventLevel.Information
                );

                return View(appointment);
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error viewing appointment details {id}: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.Details",
                    LogEventLevel.Fatal
                );
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                // Verify that the appointment belongs to the current doctor
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                
                if (doctor == null)
                {
                    return NotFound(_locService.GetSystem("Error_DoctorProfileNotFound"));
                }

                var appointment = await _appointmentService.GetAppointmentForEditAsync(id);
                if (appointment == null) return NotFound();

                LogHelper.LogWithContext(
                    $"Opened edit form for appointment {id}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.Edit",
                    LogEventLevel.Information
                );

                return View(appointment);
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error opening edit form for appointment {id}: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.Edit",
                    LogEventLevel.Fatal
                );
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AppointmentEditViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Verify that the appointment belongs to the current doctor
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                
                if (doctor == null)
                {
                    return NotFound(_locService.GetSystem("Error_DoctorProfileNotFound"));
                }

                // استخدام BaseService.UpdateAsync مباشرة
                var result = await _appointmentService.UpdateAsync<AppointmentEditViewModel>(id, model);
                if (result == null) return NotFound();

                LogHelper.LogWithContext(
                    $"Updated appointment {id}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.Edit",
                    LogEventLevel.Information
                );

                TempData["SuccessMessage"] = _locService.GetSystem("Msg_AppointmentUpdateSuccess");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error updating appointment {id}: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.Edit",
                    LogEventLevel.Fatal
                );
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Verify that the appointment belongs to the current doctor
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                
                if (doctor == null)
                {
                    return NotFound(_locService.GetSystem("Error_DoctorProfileNotFound"));
                }

                var result = await _appointmentService.DeleteAppointmentAsync(id);
                if (!result) return NotFound();

                LogHelper.LogWithContext(
                    $"Deleted appointment {id}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.Details",
                    LogEventLevel.Information
                );

                TempData["SuccessMessage"] = _locService.GetSystem("Msg_AppointmentDeleteSuccess");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error deleting appointment {id}: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.Delete",
                    LogEventLevel.Fatal
                );
                throw;
            }
        }

        [HttpGet("requests")]
        public async Task<IActionResult> Requests(string? priority = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null)
                return NotFound(_locService.GetSystem("Error_DoctorProfileNotFound"));

            var query = _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Where(a => a.DoctorId == doctor.Id && a.Status == AppointmentStatus.Pending);

            //if (!string.IsNullOrEmpty(priority))
            //    query = query.Where(a => a. == priority);

            var appointments = await query
                .OrderByDescending(a => a.ScheduledTime)
                .ToListAsync();

            var viewModel = _mapper.Map<List<AppointmentViewModel>>(appointments);

            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            // جلب بيانات الموعد والمريض
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null) return NotFound();

            // جلب كل الـ Slots المتاحة في نفس اليوم للطبيب
            var slots = await _appointmentService.GetAvailableTimeSlotsAsync(appointment.ScheduledTime.Date, appointment.DoctorId);

            // ViewModel يحتوي على بيانات الموعد والـ Slots
            var vm = new AcceptAppointmentViewModel
            {
                AppointmentId = id,
                PatientName = appointment.PatientName,
                ScheduledDate = appointment.ScheduledTime.Date,
                AvailableSlots = slots,
                SelectedSlots = new List<int> { appointment.StartSlotIndex } // افتراضي: نفس خانة الحجز
            };

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(AcceptAppointmentViewModel model)
        {
            if (model.SelectedSlots == null || !model.SelectedSlots.Any())
            {
                ModelState.AddModelError("", _locService.GetSystem("Val_RequiredTimeSlot"));
                // إعادة تحميل الـ Slots
                model.AvailableSlots = await _appointmentService.GetAvailableTimeSlotsAsync(model.ScheduledDate, model.DoctorId);
                return View(model);
            }

            // تحديث حالة الموعد وتعديل الـ Slots
            var updated = await _appointmentService.ConfirmAndUpdateSlotsAsync(model.AppointmentId, model.SelectedSlots);

            if (updated)
            {
                await _appointmentService.CancelOtherPendingAppointmentsAsync(model.AppointmentId);
                TempData["SuccessMessage"] = _locService.GetSystem("Msg_AppointmentConfirmedSlotsUpdated");
            }
            else
            {
                TempData["ErrorMessage"] = _locService.GetSystem("Error_AppointmentConfirmationFailed");
            }
            return RedirectToAction(nameof(Requests));
        }
        [HttpGet]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null) return NotFound();

            var vm = new RejectAppointmentViewModel
            {
                AppointmentId = id,
                PatientName = appointment.PatientName,
                PatientEmail = appointment.PatientEmail
            };
            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(RejectAppointmentViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.RejectReason))
            {
                ModelState.AddModelError("", _locService.GetSystem("Val_RejectionReasonRequired"));
                return View(model);
            }

            await _appointmentService.UpdateAppointmentStatusAsync(model.AppointmentId, AppointmentStatus.Cancelled);

            // إرسال الإيميل
            await _appointmentService.SendRejectionEmailAsync(model.PatientEmail, model.PatientName, model.RejectReason);

            TempData["SuccessMessage"] = _locService.GetSystem("Msg_AppointmentRejectedSuccess");
            return RedirectToAction(nameof(Requests));
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPrescription(int id)
        {
            try
            {
                var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
                if (appointment == null)
                    return NotFound();

                if (string.IsNullOrWhiteSpace(appointment.PrescriptionFileName))
                    return NotFound();

                    var (content, contentType, fileName) = await _fileService.GetPrescriptionFileAsync(appointment.PrescriptionFileName);
                    if (content == null)
                        return NotFound();

                    // Prefer inline display when possible so browser opens in a new tab
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
                    }

                    return File(content, contentType ?? "application/octet-stream");
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"Error downloading prescription: {ex.Message}",
                    User?.Identity?.Name ?? "Unknown",
                    "Doctor",
                    "AppointmentsController.DownloadPrescription",
                    LogEventLevel.Error
                );
                return NotFound();
            }
        }

[HttpGet]
public async Task<IActionResult> GetPatientSuggestions(string term)
{
    var patients = await _appointmentService.SearchPatientsAsync(term);
    
    var result = patients.Select(p => new {
        id = p.Id,
        // دمج الاسم الأول والأخير من جدول الـ User
        name = p.User != null ? $"{p.User.FirstName} {p.User.LastName}" : "Unknown", 
        phone = p.User?.PhoneNumber ?? "",
        email = p.User?.Email ?? ""
    });

    return Json(result);
}

    }
    
}



