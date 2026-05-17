using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.Helpers;
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
    public class AppointmentController(LocService locService, AppointmentService appointmentService, UserService userService, IFileService fileService, IPTS.Data.ApplicationDbContext dbContext) : Controller
    {
        private readonly LocService _locService = locService;
        private readonly AppointmentService _appointmentService = appointmentService;
        private readonly UserService _userService = userService;
        private readonly IFileService _fileService = fileService;
        private readonly IPTS.Data.ApplicationDbContext _dbContext = dbContext;
        [HttpGet("{Id}")]
        public async Task<IActionResult> Index([FromRoute] string Id, [FromQuery] DateTime? date)
        {
            if (string.IsNullOrEmpty(Id))
                return NotFound();

            var selectedDate = (date ?? DateTime.Now).Date;
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            LogHelper.LogWithContext(
                $"Appointment.Index start. doctorUserId={Id}, selectedDate={selectedDate:yyyy-MM-dd}, currentUserId={currentUserId}",
                currentUserId,
                "patient",
                "PatientAppointment.Index",
                Serilog.Events.LogEventLevel.Information);

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
                    LogHelper.LogWithContext(
                        $"Appointment.Index patient resolved. patientId={patientId}, doctorId={doctor!.Id}, doctorUserId={Id}",
                        currentUserId,
                        "patient",
                        "PatientAppointment.Index",
                        Serilog.Events.LogEventLevel.Debug);

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
                doctorId: doctor!.Id,
                doctorTimeZoneId: "W. Europe Standard Time"
            );

            LogHelper.LogWithContext(
                $"Appointment.Index slots built. doctorId={doctor.Id}, selectedDate={selectedDate:yyyy-MM-dd}, slots={timeSlots?.Count ?? 0}",
                currentUserId,
                "patient",
                "PatientAppointment.Index",
                Serilog.Events.LogEventLevel.Debug);

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
            const int maxSlotsPerAppointment = 4;
            var selectedDate = model.ScheduledDate == default ? DateTime.Now.Date : model.ScheduledDate.Date;
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            var selectedSlots = (model.SelectedSlotIndices ?? new List<int>())
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (!selectedSlots.Any() && model.SlotIndex >= 0)
            {
                selectedSlots.Add(model.SlotIndex);
            }

            var startSlotIndex = selectedSlots.Any() ? selectedSlots.Min() : -1;
            var endSlotIndex = selectedSlots.Any() ? selectedSlots.Max() : -1;

            LogHelper.LogWithContext(
                $"Appointment.Book start. doctorId={doctorId}, patientId={model.PatientId}, selectedSlots={string.Join(',', selectedSlots)}, model.Time='{model.Time}', scheduledDate={selectedDate:yyyy-MM-dd}",
                currentUserId,
                "patient",
                "PatientAppointment.Book",
                Serilog.Events.LogEventLevel.Information);

            // جلب كيان الطبيب مباشرة من DbContext المحقون
            var doctorEntity = await _dbContext.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctorEntity == null || string.IsNullOrEmpty(doctorEntity.UserId))
                return NotFound();

            var doctorUserId = doctorEntity.UserId;
            // ثم جلب DoctorViewModel من UserService باستخدام UserId (string)
            var doctor = await _userService.GetByIdAsync<string, DoctorViewModel>(doctorUserId, q => q.Include(u => u.Doctor));

            if (!ModelState.IsValid)
            {
                var modelErrors = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(msg => !string.IsNullOrWhiteSpace(msg)));

                LogHelper.LogWithContext(
                    $"Appointment.Book model state has non-blocking errors. doctorId={doctorId}, selectedSlots={string.Join(',', selectedSlots)}, errors='{modelErrors}'",
                    currentUserId,
                    "patient",
                    "PatientAppointment.Book",
                    Serilog.Events.LogEventLevel.Warning);
            }

            if (!selectedSlots.Any() || selectedSlots.Count > maxSlotsPerAppointment)
            {
                LogHelper.LogWithContext(
                    $"Appointment.Book invalid selected slot count. doctorId={doctorId}, count={selectedSlots.Count}",
                    currentUserId,
                    "patient",
                    "PatientAppointment.Book",
                    Serilog.Events.LogEventLevel.Warning);

                TempData["WarningMessage"] = _locService.GetSystem("Warn_SelectValidTimeSlot");
                return RedirectToAction(nameof(Index), new { Id = doctorUserId, date = selectedDate.ToString("yyyy-MM-dd") });
            }

            var isContiguous = selectedSlots.Zip(selectedSlots.Skip(1), (a, b) => b - a).All(diff => diff == 1);
            if (!isContiguous)
            {
                LogHelper.LogWithContext(
                    $"Appointment.Book non-contiguous slots. doctorId={doctorId}, slots={string.Join(',', selectedSlots)}",
                    currentUserId,
                    "patient",
                    "PatientAppointment.Book",
                    Serilog.Events.LogEventLevel.Warning);

                TempData["WarningMessage"] = _locService.GetSystem("Warn_SelectValidTimeSlot");
                return RedirectToAction(nameof(Index), new { Id = doctorUserId, date = selectedDate.ToString("yyyy-MM-dd") });
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var patientId = await _userService.GetByIdAsync(userId, q => q.Include(u=>u.Patient));
            if (patientId == null || patientId.Patient == null)
            {
                LogHelper.LogWithContext(
                    $"Appointment.Book patient profile not found. doctorId={doctorId}, currentUserId={currentUserId}",
                    currentUserId,
                    "patient",
                    "PatientAppointment.Book",
                    Serilog.Events.LogEventLevel.Error);

                TempData["ErrorMessage"] = _locService.GetSystem("Error_PatientProfileNotFound");
                return RedirectToAction(nameof(Index), new { Id = doctorUserId, date = selectedDate.ToString("yyyy-MM-dd") });
            }

            // تعبئة الحقول
            model.PatientId = patientId.Patient.Id;
            model.DoctorId = doctorId;

            // صارم: نتوقع أن المتصفح يرسل قيمة UTC ISO في model.Time — إذا لم تكن موجودة ارفض الطلب
            if (string.IsNullOrWhiteSpace(model.Time) || !DateTimeOffset.TryParse(model.Time, out var dto))
            {
                LogHelper.LogWithContext(
                    $"Appointment.Book missing/invalid UTC time. doctorId={doctorId}, slots={startSlotIndex}-{endSlotIndex}, rawTime='{model.Time}'",
                    currentUserId,
                    "patient",
                    "PatientAppointment.Book",
                    Serilog.Events.LogEventLevel.Error);

                TempData["WarningMessage"] = _locService.GetSystem("Error_InvalidOrMissingTime");
                return RedirectToAction(nameof(Index), new { Id = doctorUserId, date = selectedDate.ToString("yyyy-MM-dd") });
            }

            // استخدم التاريخ بالـ UTC (للفحوصات) — لا نعتمد على fallback
            var scheduledDateUtc = DateTime.SpecifyKind(dto.UtcDateTime.Date, DateTimeKind.Utc);
            model.ScheduledDate = scheduledDateUtc;

            LogHelper.LogWithContext(
                $"Appointment.Book parsed UTC time. utc={dto.UtcDateTime:o}, scheduledDateUtc={scheduledDateUtc:yyyy-MM-dd}, slots={startSlotIndex}-{endSlotIndex}",
                currentUserId,
                "patient",
                "PatientAppointment.Book",
                Serilog.Events.LogEventLevel.Debug);

            // تحقق التوفر لكل الخانات المحددة
            foreach (var slot in selectedSlots)
            {
                var available = await _appointmentService.IsSlotAvailableAsync(model.ScheduledDate, model.DoctorId, slot);
                if (!available)
                {
                    LogHelper.LogWithContext(
                        $"Appointment.Book slot unavailable. doctorId={doctorId}, utcDate={scheduledDateUtc:yyyy-MM-dd}, slotIndex={slot}",
                        currentUserId,
                        "patient",
                        "PatientAppointment.Book",
                        Serilog.Events.LogEventLevel.Warning);

                    TempData["ErrorMessage"] = _locService.GetSystem("Error_SlotNoLongerAvailable");
                    return RedirectToAction(nameof(Index), new { Id = doctorUserId, date = selectedDate.ToString("yyyy-MM-dd") });
                }
            }

            foreach (var slot in selectedSlots)
            {
                var hasPending = await _appointmentService.HasPendingAppointmentAsync(
                    model.PatientId, model.DoctorId, model.ScheduledDate, slot);

                if (hasPending)
                {
                    LogHelper.LogWithContext(
                        $"Appointment.Book duplicate pending appointment found. patientId={model.PatientId}, doctorId={model.DoctorId}, utcDate={scheduledDateUtc:yyyy-MM-dd}, slotIndex={slot}",
                        currentUserId,
                        "patient",
                        "PatientAppointment.Book",
                        Serilog.Events.LogEventLevel.Warning);

                    TempData["ErrorMessage"] = _locService.GetSystem("Error_AlreadyHasPendingAppointment");
                    return RedirectToAction(nameof(Index), new { Id = doctorUserId, date = selectedDate.ToString("yyyy-MM-dd") });
                }
            }

            model.SelectedSlotIndices = selectedSlots;
            model.SlotIndex = startSlotIndex;

            // إنشاء الموعد لخانة واحدة
            var success = await _appointmentService.CreateSingleSlotAppointmentAsync(model);
            if (!success)
            {
                LogHelper.LogWithContext(
                    $"Appointment.Book creation failed. doctorId={doctorId}, patientId={model.PatientId}, utcTime='{model.Time}', slots={startSlotIndex}-{endSlotIndex}",
                    currentUserId,
                    "patient",
                    "PatientAppointment.Book",
                    Serilog.Events.LogEventLevel.Error);

                TempData["ErrorMessage"] = _locService.GetSystem("Error_AppointmentBookingFailed");
                return RedirectToAction(nameof(Index), new { Id = doctorUserId, date = selectedDate.ToString("yyyy-MM-dd") });
            }

            LogHelper.LogWithContext(
                $"Appointment.Book success. doctorId={doctorId}, patientId={model.PatientId}, storedUtc='{model.Time}', slots={startSlotIndex}-{endSlotIndex}",
                currentUserId,
                "patient",
                "PatientAppointment.Book",
                Serilog.Events.LogEventLevel.Information);

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

            LogHelper.LogWithContext(
                $"Appointment.Appointments start. userId={userId}",
                userId,
                "patient",
                "PatientAppointment.Appointments",
                Serilog.Events.LogEventLevel.Information);

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

            LogHelper.LogWithContext(
                $"Appointment.Appointments loaded. patientId={patient.Patient.Id}, count={appointments?.Count ?? 0}",
                userId,
                "patient",
                "PatientAppointment.Appointments",
                Serilog.Events.LogEventLevel.Debug);

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

        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id, [FromQuery] DateTime? date)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userService.GetByIdAsync(userId, q => q.Include(u => u.Patient));
            if (user?.Patient == null)
                return Unauthorized();

            var appointment = await _appointmentService.GetByIdAsync(id, a => a
                .Include(x => x.Doctor).ThenInclude(d => d!.User)
            );

            if (appointment == null || appointment.PatientId != user.Patient.Id)
                return Unauthorized();

            var isPast = appointment.ScheduledTime <= DateTime.UtcNow;
            if (appointment.Status != AppointmentStatus.Pending || isPast)
            {
                TempData["WarningMessage"] = _locService.GetSystem("Warn_BookingFailedContactClinic");
                return RedirectToAction(nameof(Appointments));
            }

            var selectedDate = (date ?? appointment.ScheduledTime).Date;
            var timeSlots = await _appointmentService.GetAvailableTimeSlotsAsync(
                selectedDate,
                appointment.DoctorId,
                doctorTimeZoneId: "W. Europe Standard Time");
            var isOriginalDate = selectedDate == appointment.ScheduledTime.Date;
            var selectedSlotIndex = isOriginalDate ? appointment.StartSlotIndex : -1;
            var selectedSlotIndices = isOriginalDate
                ? Enumerable.Range(appointment.StartSlotIndex, (appointment.EndSlotIndex - appointment.StartSlotIndex) + 1).ToList()
                : new List<int>();
            var selectedTime = isOriginalDate
                ? selectedDate.AddMinutes(appointment.StartSlotIndex * 20).ToString("HH:mm")
                : string.Empty;

            var model = new PatientAppointmentEditViewModel
            {
                Id = appointment.Id,
                DoctorId = appointment.DoctorId,
                DoctorName = $"{appointment.Doctor?.User?.FirstName} {appointment.Doctor?.User?.LastName}".Trim(),
                ScheduledDate = selectedDate,
                SlotIndex = selectedSlotIndex,
                SelectedSlotIndices = selectedSlotIndices,
                Time = selectedTime,
                Notes = appointment.Notes ?? string.Empty,
                ExistingPrescriptionFileName = appointment.PrescriptionFileName
            };

            ViewBag.TimeSlots = timeSlots;
            return View(model);
        }

        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PatientAppointmentEditViewModel model)
        {
            const int maxSlotsPerAppointment = 4;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var selectedSlots = (model.SelectedSlotIndices ?? new List<int>())
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (!selectedSlots.Any() && model.SlotIndex >= 0)
            {
                selectedSlots.Add(model.SlotIndex);
            }

            var user = await _userService.GetByIdAsync(userId, q => q.Include(u => u.Patient));
            if (user?.Patient == null)
                return Unauthorized();

            var appointment = await _appointmentService.GetByIdAsync(id, a => a
                .Include(x => x.Doctor).ThenInclude(d => d!.User)
            );

            if (appointment == null || appointment.PatientId != user.Patient.Id)
                return Unauthorized();

            var isPast = appointment.ScheduledTime <= DateTime.UtcNow;
            if (appointment.Status != AppointmentStatus.Pending || isPast)
            {
                TempData["WarningMessage"] = _locService.GetSystem("Warn_BookingFailedContactClinic");
                return RedirectToAction(nameof(Appointments));
            }

            if (!selectedSlots.Any() || selectedSlots.Count > maxSlotsPerAppointment)
            {
                TempData["ErrorMessage"] = _locService.GetSystem("Warn_SelectValidTimeSlot");
                return RedirectToAction(nameof(Edit), new { id });
            }

            var isContiguous = selectedSlots.Zip(selectedSlots.Skip(1), (a, b) => b - a).All(diff => diff == 1);
            if (!isContiguous)
            {
                TempData["ErrorMessage"] = _locService.GetSystem("Warn_SelectValidTimeSlot");
                return RedirectToAction(nameof(Edit), new { id });
            }

            // Same strict UTC policy as booking: browser must send UTC ISO in model.Time.
            if (string.IsNullOrWhiteSpace(model.Time) || !DateTimeOffset.TryParse(model.Time, out var dto))
            {
                TempData["WarningMessage"] = _locService.GetSystem("Error_InvalidOrMissingTime");
                return RedirectToAction(nameof(Edit), new { id });
            }

            var scheduledDateUtc = DateTime.SpecifyKind(dto.UtcDateTime.Date, DateTimeKind.Utc);
            foreach (var slot in selectedSlots)
            {
                var isAvailable = await _appointmentService.IsSlotAvailableAsync(scheduledDateUtc, appointment.DoctorId, slot);
                if (!isAvailable)
                {
                    TempData["ErrorMessage"] = _locService.GetSystem("Error_SlotNoLongerAvailable");
                    return RedirectToAction(nameof(Edit), new { id });
                }
            }

            if (model.PrescriptionFile != null && model.PrescriptionFile.Length > 0)
            {
                var (isValid, errorMessage) = _fileService.ValidatePrescriptionFile(model.PrescriptionFile);
                if (!isValid)
                {
                    TempData["ErrorMessage"] = errorMessage;
                    return RedirectToAction(nameof(Edit), new { id });
                }

                if (!string.IsNullOrWhiteSpace(appointment.PrescriptionFileName))
                {
                    await _fileService.DeletePrescriptionFileAsync(appointment.PrescriptionFileName);
                }

                appointment.PrescriptionFileName = await _fileService.SavePrescriptionFileAsync(model.PrescriptionFile);
                if (string.IsNullOrWhiteSpace(appointment.PrescriptionFileName))
                {
                    TempData["ErrorMessage"] = _locService.GetSystem("Error_AppointmentBookingFailed");
                    return RedirectToAction(nameof(Edit), new { id });
                }
            }
            else if (model.RemovePrescription && !string.IsNullOrWhiteSpace(appointment.PrescriptionFileName))
            {
                await _fileService.DeletePrescriptionFileAsync(appointment.PrescriptionFileName);
                appointment.PrescriptionFileName = null;
            }

            appointment.ScheduledTime = dto.UtcDateTime;
            appointment.StartSlotIndex = selectedSlots.Min();
            appointment.EndSlotIndex = selectedSlots.Max();
            appointment.Notes = model.Notes ?? string.Empty;

            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = _locService.GetSystem("Msg_AppointmentUpdateSuccess");
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