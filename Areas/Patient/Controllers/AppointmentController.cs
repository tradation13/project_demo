using IPTS.Models.Entites;
using IPTS.Services;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IPTS.Areas.Patient.Controllers
{
    [Area("patient")]
    [Authorize(Roles = "patient")]
    [Route("[area]/[controller]")]
    public class AppointmentController(AppointmentService appointmentService, UserService userService) : Controller
    {
        private readonly AppointmentService _appointmentService = appointmentService;
        private readonly UserService _userService = userService;

        [HttpGet("{Id}")]
        public async Task<IActionResult> Index([FromRoute] string Id, [FromQuery] DateTime? date)
        {
            var doctor = await _userService.GetByIdAsync<string, DoctorViewModel>(
                Id, q => q.Include(u => u.Doctor));

            if (doctor is null) return NotFound();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var patientUser = await _userService.GetByIdAsync(userId, q => q.Include(u => u.Patient));
                var patientId = patientUser?.Patient?.Id;

                if (patientId != null)
                {
                    var hasAnyWithSameDoctor = await _appointmentService.IsExistAsync(
                        a => (a.PatientId == patientId && a.DoctorId == doctor.Id) &&( a.Status == AppointmentStatus.Pending || a.ScheduledTime > DateTime.UtcNow)
                    );

                    if (hasAnyWithSameDoctor)
                    {
                        TempData["InfoMessage"] = $"You already have a previous appointment with Dr. {doctor.FullName}. You have been redirected to your appointments page.";
                        return RedirectToAction(nameof(Appointments));
                    }
                }
            }

            var selectedDate = (date ?? DateTime.Now).Date;
            var timeSlots = await _appointmentService.GetAvailableTimeSlotsAsync(
                dateLocal: selectedDate,
                doctorId: doctor.Id
            );

            var vm = new DoctorScheduleViewModel
            {
                Doctor = doctor,
                TimeSlots = timeSlots ?? new(),
                SelectedDate = selectedDate
            };

            return View(vm);
        }
        [HttpPost("{doctorId}/book")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book([FromRoute] int doctorId, SingleAppointmentCreateViewModel model)
        {
            var selectedDate = model.ScheduledDate == default ? DateTime.Now.Date : model.ScheduledDate.Date;

            if (!ModelState.IsValid)
            {
                TempData["WarningMessage"] = "Please fill all required fields.";
                return RedirectToAction(nameof(Index), new { doctorId, date = selectedDate.ToString("yyyy-MM-dd") });
            }

            if (model.SlotIndex < 0)
            {
                TempData["WarningMessage"] = "Please select one valid time slot.";
                return RedirectToAction(nameof(Index), new { doctorId, date = selectedDate.ToString("yyyy-MM-dd") });
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var patientId = await _userService.GetByIdAsync(userId, q => q.Include(u=>u.Patient));
            if (patientId == null)
            {
                TempData["ErrorMessage"] = "Patient profile not found.";
                return RedirectToAction(nameof(Index), new { doctorId, date = selectedDate.ToString("yyyy-MM-dd") });
            }

            // تعبئة الحقول
            model.PatientId = patientId.Patient.Id;
            model.DoctorId = doctorId;
            model.ScheduledDate = DateTime.SpecifyKind(selectedDate, DateTimeKind.Utc);

            // تحقق التوفر
            var available = await _appointmentService.IsSlotAvailableAsync(model.ScheduledDate, model.DoctorId, model.SlotIndex);
            if (!available)
            {
                TempData["ErrorMessage"] = "Selected time slot is no longer available.";
                return RedirectToAction(nameof(Index), new { doctorId, date = selectedDate.ToString("yyyy-MM-dd") });
            }

            var hasPending = await _appointmentService.HasPendingAppointmentAsync(
             model.PatientId, model.DoctorId, model.ScheduledDate, model.SlotIndex);

            if (hasPending)
            {
                TempData["ErrorMessage"] = "You already have a pending appointment for this time slot with this doctor.";
                return RedirectToAction(nameof(Index), new { doctorId, date = selectedDate.ToString("yyyy-MM-dd") });
            }

            // إنشاء الموعد لخانة واحدة
            var success = await _appointmentService.CreateSingleSlotAppointmentAsync(model);
            if (!success)
            {
                TempData["ErrorMessage"] = "Failed to book appointment. Please try again.";
                return RedirectToAction(nameof(Index), new { doctorId, date = selectedDate.ToString("yyyy-MM-dd") });
            }

            TempData["SuccessMessage"] = $"Appointment booked for {model.Time} (20 minutes).";
            return RedirectToAction(
                "Appointments",
                "Appointment",                 
                new { area = "Patient", doctorId, date = selectedDate.ToString("yyyy-MM-dd") }
            );
        }
        
        [HttpGet("my-appointments")]
        public async Task<IActionResult> Appointments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Get the patient entity for the current user
            var patient = await _userService.GetByIdAsync(userId, q => q.Include(u => u.Patient));
            if (patient?.Patient == null)
                return NotFound("Patient profile not found.");

            // Fetch appointments for this patient
            var appointments = await _appointmentService.GetAllAsync<AppointmentViewModel>(
                 a =>
                 a.Include(ap => ap.Doctor).ThenInclude(d => d.User)
                .Where(a => a.PatientId == patient.Patient.Id)
                .OrderByDescending(a => a.ScheduledTime)
            );

            return View(appointments);
        }
        [HttpGet("my-appointments/{id:int}")]
        public async Task<IActionResult> AppointmentDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userService.GetByIdAsync(userId, q => q.Include(u => u.Patient));
            if (user?.Patient == null) return NotFound("Patient profile not found.");

            // Fetch the entity (not a VM)
            var appt = await _appointmentService.GetByIdAsync(id, a => a
                .Include(x => x.Doctor).ThenInclude(d => d.User)
                .Include(x => x.Patient).ThenInclude(p => p.User)
            );

            if (appt == null) return NotFound();

            // Map entity -> ViewModel (manual mapping shown; use AutoMapper if you prefer)
            var vm = new AppointmentViewModel
            {
                Id = appt.Id,

                // Patient
                PatientName = appt.Patient?.User?.FirstName + " " + appt.Patient?.User?.LastName ?? appt.Patient?.User?.UserName ?? string.Empty,
                PatientIdentityNumber = appt.Patient?.IdentityNumber ?? string.Empty,
                PatientPhone = appt.Patient?.User?.PhoneNumber ?? string.Empty,
                PatientEmail = appt.Patient?.User?.Email ?? string.Empty,

                // Doctor
                DoctorId = appt.DoctorId,
                DoctorName = appt.Patient?.User?.FirstName + " " + appt.Patient?.User?.LastName ?? appt.Doctor?.User?.UserName ?? string.Empty,

                // Time/Status/Notes
                ScheduledTime = appt.ScheduledTime,
                Status = appt.Status,
                Notes = appt.Notes ?? string.Empty,

                // Slots (assuming these exist on Appointment)
                StartSlotIndex = appt.StartSlotIndex,
                EndSlotIndex = appt.EndSlotIndex
            };

            return View("AppointmentDetails", vm);
        }

        [HttpGet("Edit")]
        public IActionResult Edit()
        {
            TempData["WarningMessage"] = "Failed to book appointment. Please contact the clinic directly to modify or reschedule your appointment.";
            return RedirectToAction(nameof(Appointments));

        }
    }
}

