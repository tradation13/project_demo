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
namespace IPTS.Areas.Doctor.Controllers
{
    [Area("doctor")]
    [Authorize(Roles = "doctor")]
    public class AppointmentsController(AppointmentService appointmentService, IMapper mapper, UserManager<AppUser> userManager, ApplicationDbContext context, UserService userService) : Controller
    {
        private readonly AppointmentService _appointmentService = appointmentService;
        private readonly IMapper _mapper = mapper;
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly ApplicationDbContext _context = context;
        private readonly UserService _userService = userService;

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
        public async Task<IActionResult> SearchPatient([FromForm] string? IdentityNumber, [FromForm] string? PhoneNumber, [FromForm] string? Email)
        {
            try
            {
                // Check if at least one search criteria is provided  
                if (string.IsNullOrWhiteSpace(IdentityNumber) &&
                    string.IsNullOrWhiteSpace(PhoneNumber) &&
                    string.IsNullOrWhiteSpace(Email))
                {
                    TempData["ErrorMessage"] = "Please provide at least one search criteria (Identity Number, Phone Number, or Email).";
                    return RedirectToAction("SearchPatient");
                }

                IPTS.Models.Entites.Patient? patient = null;

                // Search by Identity Number first (most reliable)  
                if (!string.IsNullOrWhiteSpace(IdentityNumber))
                {
                    patient = await _appointmentService.SearchPatientByIdentityNumberAsync(IdentityNumber);
                }

                // If not found by identity, search by phone  
                if (patient == null && !string.IsNullOrWhiteSpace(PhoneNumber))
                {
                    patient = await _appointmentService.SearchPatientByPhoneAsync(PhoneNumber);
                }

                // If still not found, search by email  
                if (patient == null && !string.IsNullOrWhiteSpace(Email))
                {
                    patient = await _appointmentService.SearchPatientByEmailAsync(Email);
                }

                if (patient == null)
                {
                    TempData["WarningMessage"] = "Patient not found with the provided information. You can create a new patient.";
                    TempData["SearchData"] = new { IdentityNumber, PhoneNumber, Email };
                    return RedirectToAction("CreatePatient");
                }

                // Patient found, redirect to appointment scheduling  
                TempData["PatientId"] = patient.Id;
                TempData["PatientName"] = $"{patient.User?.FirstName} {patient.User?.LastName}".Trim();
                if (string.IsNullOrEmpty(TempData["PatientName"].ToString()))
                {
                    TempData["PatientName"] = patient.User?.UserName ?? "Unknown";
                }
                TempData["SuccessMessage"] = $"Patient found: {TempData["PatientName"]}. Proceeding to appointment scheduling.";

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
                throw;
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
                
                TempData["InfoMessage"] = "Create a new patient account. All fields marked with * are required.";
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
                    TempData["WarningMessage"] = "Please correct the errors in the form.";
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
                        IdentityNumber = model.IdentityNumber,
                        BirthDate = model.BirthDate
                    }
                };

                // Use UserService to create the patient
                var result = await _userService.RegisterAsync(registerModel);
                
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Patient created successfully! You can now search for them to schedule an appointment.";
                    return RedirectToAction("SearchPatient");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    TempData["WarningMessage"] = "Please correct the errors in the form.";
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
                TempData["ErrorMessage"] = $"Error creating patient: {ex.Message}";
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
                    TempData["WarningMessage"] = "No patient selected. Please search for a patient first.";
                    return RedirectToAction("SearchPatient");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                
                if (doctor == null)
                {
                    TempData["ErrorMessage"] = "Doctor profile not found.";
                    return NotFound("Doctor profile not found");
                }

                var model = new AppointmentCreateViewModel
                {
                    PatientId = patientId.Value,
                    DoctorId = doctor.Id,
                    ScheduledDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc)
                };

                TempData["InfoMessage"] = "Select date and time slots for the appointment.";
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
                    TempData["WarningMessage"] = "Please fill all required fields.";
                    return View(model);
                }

                // Validate time slot selection
                if (model.StartSlotIndex < 0 || model.EndSlotIndex < 0 || model.StartSlotIndex > model.EndSlotIndex)
                {
                    TempData["WarningMessage"] = "Please select valid time slots.";
                    return View(model);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
                
                if (doctor == null)
                {
                    TempData["ErrorMessage"] = "Doctor profile not found.";
                    return NotFound("Doctor profile not found");
                }

                model.DoctorId = doctor.Id;
                
                // Ensure the scheduled date is in UTC format
                if (model.ScheduledDate.Kind != DateTimeKind.Utc)
                {
                    model.ScheduledDate = DateTime.SpecifyKind(model.ScheduledDate, DateTimeKind.Utc);
                }
                
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
                    TempData["SuccessMessage"] = $"Appointment created successfully! Duration: {totalDuration} minutes ({model.StartTime} - {model.EndTime}). One appointment created covering all selected time slots.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create appointment. Please try again.";
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
                    return Json(new { success = false, message = "Doctor profile not found" });
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
                return Json(new { success = false, message = "Error loading time slots" });
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
                    return NotFound("Doctor profile not found");
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
                    return NotFound("Doctor profile not found");
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
                    return NotFound("Doctor profile not found");
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

                TempData["SuccessMessage"] = "Appointment updated successfully.";
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
                    return NotFound("Doctor profile not found");
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

                TempData["SuccessMessage"] = "Appointment deleted successfully.";
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
                return NotFound("Doctor profile not found.");

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
                ModelState.AddModelError("", "Please select at least one time slot.");
                // إعادة تحميل الـ Slots
                model.AvailableSlots = await _appointmentService.GetAvailableTimeSlotsAsync(model.ScheduledDate, model.DoctorId);
                return View(model);
            }

            // تحديث حالة الموعد وتعديل الـ Slots
            var updated = await _appointmentService.ConfirmAndUpdateSlotsAsync(model.AppointmentId, model.SelectedSlots);

            if (updated)
            {
                await _appointmentService.CancelOtherPendingAppointmentsAsync(model.AppointmentId);
                TempData["SuccessMessage"] = "Appointment confirmed and slots updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to confirm appointment. Please try again.";
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
                ModelState.AddModelError("", "Please provide a reason for rejection.");
                return View(model);
            }

            await _appointmentService.UpdateAppointmentStatusAsync(model.AppointmentId, AppointmentStatus.Cancelled);

            // إرسال الإيميل
            await _appointmentService.SendRejectionEmailAsync(model.PatientEmail, model.PatientName, model.RejectReason);

            TempData["SuccessMessage"] = "Appointment rejected and reason sent to the patient.";
            return RedirectToAction(nameof(Requests));
        }
    }
}
