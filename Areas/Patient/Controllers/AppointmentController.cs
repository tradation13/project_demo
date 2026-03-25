using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.Resources;
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
    public class AppointmentController(LocService locService, AppointmentService appointmentService, UserService userService, IFileService fileService) : Controller
    {
        private readonly LocService _locService = locService;
        private readonly AppointmentService _appointmentService = appointmentService;
        private readonly UserService _userService = userService;
        private readonly IFileService _fileService = fileService;
        [HttpGet("{Id}")]
        public async Task<IActionResult> Index([FromRoute] string Id, [FromQuery] DateTime? date)
        {
            if (string.IsNullOrEmpty(Id))
                return NotFound();

            var selectedDate = (date ?? DateTime.Now).Date;

            // Get doctor by UserId
            var doctor = await _userService.GetByIdAsync<string, DoctorViewModel>(
                Id, q => q.Include(u => u.Doctor));

            if (doctor?.Id <= 0)
                return NotFound();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var patientUser = await _userService.GetByIdAsync(userId, q => q.Include(u => u.Patient));
                var patientId = patientUser?.Patient?.Id;

                if (patientId != null)
                {
                    var hasAnyWithSameDoctor = await _appointmentService.IsExistAsync(
                        a => (a.PatientId == patientId && a.DoctorId == doctor!.Id) &&( a.Status == AppointmentStatus.Pending || a.ScheduledTime > DateTime.UtcNow)
                    );

                    if (hasAnyWithSameDoctor)
                    {
                        TempData["InfoMessage"] = string.Format(
    _locService.GetSystem("Msg_AlreadyHasAppointmentRedirect"), 
    doctor!.FullName
);
                        return RedirectToAction(nameof(Appointments));
                    }
                }
            }

            var timeSlots = await _appointmentService.GetAvailableTimeSlotsAsync(
                dateLocal: selectedDate,
                doctorId: doctor!.Id
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

            // Get doctor to get doctor UserId for redirects
            var appointments = await _appointmentService.GetAllAsync(q => q
                .Include(a => a.Doctor)
                .Where(a => a.DoctorId == doctorId)
                .Take(1));

            var appointment = appointments.FirstOrDefault();
            if (appointment?.Doctor?.UserId is null)
                return NotFound();

            var doctorUserId = appointment.Doctor.UserId;

            if (!ModelState.IsValid)
            {
                TempData["WarningMessage"] = _locService.GetSystem("Warn_FillRequiredFields");
                return RedirectToAction(nameof(Index), new { Id = doctorUserId, date = selectedDate.ToString("yyyy-MM-dd") });
            }

            if (model.SlotIndex < 0)
            {
                TempData["WarningMessage"] = _locService.GetSystem("Warn_SelectValidTimeSlot");
                return RedirectToAction(nameof(Index), new { Id = doctorUserId, date = selectedDate.ToString("yyyy-MM-dd") });
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var patientId = await _userService.GetByIdAsync(userId, q => q.Include(u=>u.Patient));
            if (patientId == null || patientId.Patient == null)
            {
                TempData["ErrorMessage"] = _locService.GetSystem("Error_PatientProfileNotFound");
                return RedirectToAction(nameof(Index), new { Id = doctorUserId, date = selectedDate.ToString("yyyy-MM-dd") });
            }

            // تعبئة الحقول
            model.PatientId = patientId.Patient.Id;
            model.DoctorId = doctorId;
            model.ScheduledDate = DateTime.SpecifyKind(selectedDate, DateTimeKind.Utc);

            // تحقق التوفر
            var available = await _appointmentService.IsSlotAvailableAsync(model.ScheduledDate, model.DoctorId, model.SlotIndex);
            if (!available)
            {
                TempData["ErrorMessage"] = _locService.GetSystem("Error_SlotNoLongerAvailable");
                return RedirectToAction(nameof(Index), new { Id = doctorUserId, date = selectedDate.ToString("yyyy-MM-dd") });
            }

            var hasPending = await _appointmentService.HasPendingAppointmentAsync(
             model.PatientId, model.DoctorId, model.ScheduledDate, model.SlotIndex);

            if (hasPending)
            {
                TempData["ErrorMessage"] = _locService.GetSystem("Error_AlreadyHasPendingAppointment");
                return RedirectToAction(nameof(Index), new { Id = doctorUserId, date = selectedDate.ToString("yyyy-MM-dd") });
            }

            // إنشاء الموعد لخانة واحدة
            var success = await _appointmentService.CreateSingleSlotAppointmentAsync(model);
            if (!success)
            {
                TempData["ErrorMessage"] = _locService.GetSystem("Error_AppointmentBookingFailed");
                return RedirectToAction(nameof(Index), new { Id = doctorUserId, date = selectedDate.ToString("yyyy-MM-dd") });
            }

            TempData["SuccessMessage"] = string.Format(
    _locService.GetSystem("Msg_AppointmentBookedWithDuration"), 
    model.Time
);
            return RedirectToAction(
                "Appointments",
                "Appointment",                 
                new { area = "Patient" }
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
                return NotFound(_locService.GetSystem("Error_PatientProfileNotFound"));

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
            if (user?.Patient == null) return NotFound(_locService.GetSystem("Error_PatientProfileNotFound"));

            // Fetch the entity (not a VM)
            var appt = await _appointmentService.GetByIdAsync(id, a => a
                .Include(x => x.Doctor).ThenInclude(d => d!.User)
                .Include(x => x.Patient).ThenInclude(p => p!.User)
            );

            if (appt == null) return NotFound();

            // Map entity -> ViewModel (manual mapping shown; use AutoMapper if you prefer)
            var vm = new AppointmentViewModel
            {
                Id = appt.Id,

                // Patient
                PatientName = appt.Patient?.User?.FirstName + " " + appt.Patient?.User?.LastName ?? appt.Patient?.User?.UserName ?? string.Empty,
                // PatientIdentityNumber = appt.Patient?.IdentityNumber ?? string.Empty,
                PatientPhone = appt.Patient?.User?.PhoneNumber ?? string.Empty,
                PatientEmail = appt.Patient?.User?.Email ?? string.Empty,

                // Doctor
                DoctorId = appt.DoctorId,
                DoctorName = appt.Doctor?.User?.FirstName + " " + appt.Doctor?.User?.LastName ?? appt.Doctor?.User?.UserName ?? string.Empty,

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
            TempData["WarningMessage"] = _locService.GetSystem("Warn_BookingFailedContactClinic");
            return RedirectToAction(nameof(Appointments));
        }

        [HttpGet("download-prescription/{id:int}")]
        public async Task<IActionResult> DownloadPrescription(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userService.GetByIdAsync(userId, q => q.Include(u => u.Patient));
            if (user?.Patient == null)
                return Unauthorized();

            // Get appointment and verify ownership
            var appointment = await _appointmentService.GetByIdAsync(id);
            if (appointment == null || appointment.PatientId != user.Patient.Id)
                return Unauthorized();

            if (string.IsNullOrEmpty(appointment.PrescriptionFileName))
                return NotFound(_locService.GetSystem("Error_PrescriptionNotFound"));

            // Get file
            var (fileBytes, contentType, fileName) = await _fileService.GetPrescriptionFileAsync(appointment.PrescriptionFileName);
            if (fileBytes == null)
                return NotFound(_locService.GetSystem("Error_PrescriptionNotFound"));

            // Serve inline so browser displays instead of forcing download
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            return File(fileBytes, contentType ?? "application/octet-stream");
        }

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userService.GetByIdAsync(userId, q => q.Include(u => u.Patient));
            if (user?.Patient == null)
                return Unauthorized();

            // Get appointment and verify ownership
            var appointment = await _appointmentService.GetByIdAsync(id);
            if (appointment == null || appointment.PatientId != user.Patient.Id)
                return Unauthorized();

            // Can only delete pending or cancelled appointments
            if (appointment.Status != AppointmentStatus.Pending && appointment.Status != AppointmentStatus.Cancelled)
            {
                TempData["ErrorMessage"] = _locService.GetSystem("Error_CannotDeleteConfirmedAppointment");
                return RedirectToAction(nameof(Appointments));
            }

            // Delete appointment and files
            var success = await _appointmentService.DeleteAppointmentWithFilesAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = _locService.GetSystem("Error_FailedToDeleteAppointment");
                return RedirectToAction(nameof(Appointments));
            }

            TempData["SuccessMessage"] = _locService.GetSystem("Msg_AppointmentDeletedSuccessfully");
            return RedirectToAction(nameof(Appointments));
        }
    }
}