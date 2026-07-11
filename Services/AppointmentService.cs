using AutoMapper;
using IPTS.Data;
using IPTS.Helpers;
using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.Resources;
using IPTS.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IPTS.Services
{
    public class AppointmentService(LocService locService, EmailService emailService, ApplicationDbContext context, IMapper mapper, UserService userService, IFileService fileService) : BaseService<Appointment>(context, mapper)
    {
        private readonly LocService _locService = locService;
        private readonly UserService _userService = userService;
        private readonly EmailService _emailService = emailService;
        private readonly IFileService _fileService = fileService;
        public async Task<List<AppointmentViewModel>> GetAppointmentsForDoctorAsync(string userId)
        {
            var user = await _userService.GetByIdAsync(userId, q => q.Include(u => u.Doctor));
            
            if (user?.Doctor == null)
            {
                return new List<AppointmentViewModel>(); // Return empty list if doctor not found
            }

            // Get appointments with full navigation properties
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p!.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d!.User)
                .Where(a => a.DoctorId == user.Doctor.Id)
                .OrderByDescending(a => a.ScheduledTime)
                .ToListAsync();

            // Map to ViewModel using AutoMapper
            return _mapper.Map<List<AppointmentViewModel>>(appointments);
        }
        public async Task<bool> ConfirmAndUpdateSlotsAsync(int appointmentId, List<int> selectedSlots)
        {
            var appointment = await _dbSet.FirstOrDefaultAsync(a => a.Id == appointmentId);
            if (appointment == null) return false;

            appointment.Status = AppointmentStatus.Confirmed;
            appointment.StartSlotIndex = selectedSlots.Min();
            appointment.EndSlotIndex = selectedSlots.Max();
            appointment.ScheduledTime = DateTime.SpecifyKind(
                appointment.ScheduledTime.Date.AddMinutes(appointment.StartSlotIndex * 20),
                DateTimeKind.Utc
            );

            _dbSet.Update(appointment);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task SendRejectionEmailAsync(string toEmail, string patientName, string reason)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                LogHelper.LogWithContext(
                    "Skipped rejection email: patient email is empty",
                    string.Empty,
                    "doctor",
                    "AppointmentService.SendRejectionEmailAsync",
                    Serilog.Events.LogEventLevel.Warning);
                return;
            }

            var subject = _locService.GetSystem("Email_Subject_AppointmentRejected");
            var body = string.Format(
                _locService.GetSystem("Email_Body_AppointmentRejected"),
                patientName,
                reason
            );

            await _emailService.SendEmail(toEmail, subject, body);

            LogHelper.LogWithContext(
                $"Rejection email sent to {toEmail}",
                string.Empty,
                "doctor",
                "AppointmentService.SendRejectionEmailAsync",
                Serilog.Events.LogEventLevel.Information);
        }

        public async Task SendAcceptanceEmailAsync(string toEmail, string patientName, DateTime scheduledDate, int startSlotIndex, int endSlotIndex)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                LogHelper.LogWithContext(
                    "Skipped acceptance email: patient email is empty",
                    string.Empty,
                    "doctor",
                    "AppointmentService.SendAcceptanceEmailAsync",
                    Serilog.Events.LogEventLevel.Warning);
                return;
            }

            var startTime = scheduledDate.Date.AddHours(8).AddMinutes(startSlotIndex * 20);
            var endTime = scheduledDate.Date.AddHours(8).AddMinutes((endSlotIndex + 1) * 20);
            var dateText = startTime.ToString("dd.MM.yyyy");
            var timeRange = $"{startTime:HH:mm} – {endTime:HH:mm}";

            var subject = _locService.GetSystem("Email_Subject_AppointmentAccepted");
            var body = string.Format(
                _locService.GetSystem("Email_Body_AppointmentAccepted"),
                patientName,
                dateText,
                timeRange
            );

            await _emailService.SendEmail(toEmail, subject, body);

            LogHelper.LogWithContext(
                $"Acceptance email sent to {toEmail} for {dateText} {timeRange}",
                string.Empty,
                "doctor",
                "AppointmentService.SendAcceptanceEmailAsync",
                Serilog.Events.LogEventLevel.Information);
        }
        public async Task<bool> HasPendingAppointmentAsync(int patientId, int doctorId, DateTime scheduledDate, int slotIndex)
        {
            var day = scheduledDate.Date;
            return await _dbSet.AnyAsync(a =>
                a.PatientId == patientId &&
                a.DoctorId == doctorId &&
                a.ScheduledTime.Date == day &&
                a.StartSlotIndex <= slotIndex &&
                a.EndSlotIndex >= slotIndex &&
                a.Status == AppointmentStatus.Pending
            );
        }
        public async Task<AppointmentViewModel?> GetAppointmentByIdAsync(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p!.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d!.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return null;

            return _mapper.Map<AppointmentViewModel>(appointment);
        }

        public async Task<AppointmentEditViewModel?> GetAppointmentForEditAsync(int id)
        {
            // استخدام BaseService.GetByIdAsync مع include function
            return await GetByIdAsync<int, AppointmentEditViewModel>(id);
        }

        public async Task<bool> UpdateAppointmentStatusAsync(int id, AppointmentStatus status)
        {
            // استخدام BaseService.GetByIdAsync للحصول على الكيان
            var appointment = await GetByIdAsync<int>(id);
            if (appointment == null) return false;

            appointment.Status = status;
            _dbSet.Update(appointment);
            await _context.SaveChangesAsync();
            return true;
        }

        // استخدام BaseService.DeleteAsync
        public async Task<bool> DeleteAppointmentAsync(int id)
        {
            return await DeleteAsync(id);
        }

        // استخدام BaseService.AddAsync
        public async Task<AppointmentCreateViewModel> CreateAppointmentAsync(AppointmentCreateViewModel model)
        {
            return await AddAsync(model);
        }

        // public async Task<Patient?> SearchPatientByIdentityNumberAsync(string identityNumber)
        // {
        //     return await _context.Patients
        //         .Include(p => p.User)
        //         .FirstOrDefaultAsync(p => p.IdentityNumber == identityNumber);
        // }


        public async Task<Patient?> SearchPatientByPhoneAsync(string phoneNumber)
        {
            return await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.User.PhoneNumber == phoneNumber);
        }

        public async Task<Patient?> SearchPatientByEmailAsync(string email)
        {
            return await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.User.Email == email);
        }

        public async Task<List<AppointmentTimeSlotViewModel>> GetAvailableTimeSlotsAsync(
    DateTime dateLocal, int doctorId, int slotMinutes = 20,
    string doctorTimeZoneId = null!, int leadMinutes = 0)
        {
            var resolvedTimeZoneId = string.IsNullOrWhiteSpace(doctorTimeZoneId)
                ? TimeZoneInfo.Local.Id
                : doctorTimeZoneId;

            var tz = TimeZoneInfo.FindSystemTimeZoneById(resolvedTimeZoneId);

            DateTime ToClinicUtc(DateTime localDateTime)
            {
                var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
                var offset = tz.GetUtcOffset(unspecified);
                return new DateTimeOffset(unspecified, offset).UtcDateTime;
            }

            LogHelper.LogWithContext(
                $"GetAvailableTimeSlotsAsync start. doctorId={doctorId}, dateLocal={dateLocal:yyyy-MM-dd}, tz={resolvedTimeZoneId}, slotMinutes={slotMinutes}, leadMinutes={leadMinutes}",
                string.Empty,
                "patient",
                "AppointmentService.GetAvailableTimeSlotsAsync",
                Serilog.Events.LogEventLevel.Information);

            // تاريخ اليوم المطلوب كـ "وقت حائطي" (بدون Kind) ثم ساعات العمل
            var dateWallClock = DateTime.SpecifyKind(dateLocal.Date, DateTimeKind.Unspecified);
            var workStartLocal = dateWallClock.AddHours(8);   // 08:00
            var workEndLocal = dateWallClock.AddHours(18);  // 18:00

            // نافذة اليوم بالـ UTC
            var dayStartUtc = ToClinicUtc(workStartLocal);
            var dayEndUtc = ToClinicUtc(workEndLocal);
            LogHelper.LogWithContext(
                $"Working window. workStartLocal={workStartLocal:O}, workEndLocal={workEndLocal:O}, dayStartUtc={dayStartUtc:O}, dayEndUtc={dayEndUtc:O}",
                string.Empty,
                "patient",
                "AppointmentService.GetAvailableTimeSlotsAsync",
                Serilog.Events.LogEventLevel.Debug);

            // الآن الحالي في منطقة الطبيب
            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

            // اجلب مواعيد اليوم دفعة واحدة (الممنوعة من الحجز)
            var activeStatuses = new[] { AppointmentStatus.Confirmed, AppointmentStatus.Pending };

            var todaysAppts = await _dbSet
                .AsNoTracking()
                .Where(a => a.DoctorId == doctorId
                            && activeStatuses.Contains(a.Status)
                            && a.ScheduledTime >= dayStartUtc
                            && a.ScheduledTime < dayEndUtc)
                .Select(a => new { a.StartSlotIndex, a.EndSlotIndex })
                .ToListAsync();

            LogHelper.LogWithContext(
                $"Occupied appointments loaded. doctorId={doctorId}, count={todaysAppts.Count}",
                string.Empty,
                "patient",
                "AppointmentService.GetAvailableTimeSlotsAsync",
                Serilog.Events.LogEventLevel.Debug);

            // حضّر مصفوفة السلوتس لليوم
            var totalMinutes = (int)(workEndLocal - workStartLocal).TotalMinutes;
            var slotCount = totalMinutes / slotMinutes;
            var occupied = new bool[slotCount];

            foreach (var appt in todaysAppts)
            {
                var start = Math.Clamp(appt.StartSlotIndex, 0, slotCount - 1);
                var end = Math.Clamp(appt.EndSlotIndex, 0, slotCount - 1);
                for (int i = start; i <= end; i++)
                    occupied[i] = true;
            }

            // ولِّد النتيجة مع إلغاء السلوتس الماضية لليوم الحالي
            var result = new List<AppointmentTimeSlotViewModel>(capacity: slotCount);
            bool isTodayInDoctorTZ = (nowLocal.Date == dateWallClock.Date);

            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                var timeLocal = workStartLocal.AddMinutes(slotIndex * slotMinutes);

                bool isPastNow = isTodayInDoctorTZ && timeLocal <= nowLocal.AddMinutes(leadMinutes);
                bool isAvailable = !occupied[slotIndex] && !isPastNow;

                var utcDateTime = ToClinicUtc(timeLocal);

                result.Add(new AppointmentTimeSlotViewModel
                {
                    Time = timeLocal.ToString("HH:mm"),
                    TimeUtc = utcDateTime.ToString("o"),
                    IsAvailable = isAvailable,
                    IsSelected = false,
                    SlotIndex = slotIndex
                });
            }

            return result;
        }

        public async Task<bool> CreateAppointmentWithSlotsAsync(AppointmentCreateViewModel model, List<int> selectedSlotIndices)
        {
            try
            {
                if (selectedSlotIndices == null || selectedSlotIndices.Count == 0)
                {
                    return false;
                }

                // Require UTC ISO string from client (strict, no fallback)
                if (string.IsNullOrWhiteSpace(model.Time) || !DateTimeOffset.TryParse(model.Time, out var dto))
                {
                    LogHelper.LogWithContext(
                        $"CreateAppointmentWithSlotsAsync invalid UTC input. rawTime='{model.Time}'",
                        string.Empty,
                        "doctor",
                        "AppointmentService.CreateAppointmentWithSlotsAsync",
                        Serilog.Events.LogEventLevel.Error);
                    
                    return false; // invalid request from client — require UTC time
                }

                // Calculate total duration based on selected slots
                var totalDuration = selectedSlotIndices.Count * 20; // 20 minutes per slot
                
                // Use the UTC DateTime from the parsed ISO string
                var appointmentStartTime = dto.UtcDateTime;
                
                var startSlotIndex = selectedSlotIndices.Min();
                var endSlotIndex = selectedSlotIndices.Max();

                // Create ONE appointment with the total duration
                var appointment = new Appointment
                {
                    PatientId = model.PatientId,
                    DoctorId = model.DoctorId,
                    ScheduledTime = appointmentStartTime, // UTC DateTime
                    Status = AppointmentStatus.Confirmed,
                    Notes = string.IsNullOrWhiteSpace(model.Notes) ? string.Empty : model.Notes,
                    PrescriptionFileName = string.IsNullOrWhiteSpace(model.PrescriptionFileName) ? string.Empty : model.PrescriptionFileName,
                    StartSlotIndex = startSlotIndex,
                    EndSlotIndex = endSlotIndex
                };

                LogHelper.LogWithContext(
                    $"CreateAppointmentWithSlotsAsync creating appointment. patientId={model.PatientId}, doctorId={model.DoctorId}, scheduledUtc={appointmentStartTime:O}, slots={startSlotIndex}-{endSlotIndex}",
                    string.Empty,
                    "doctor",
                    "AppointmentService.CreateAppointmentWithSlotsAsync",
                    Serilog.Events.LogEventLevel.Information);

                await _dbSet.AddAsync(appointment);
                await _context.SaveChangesAsync();
                
                LogHelper.LogWithContext(
                    $"CreateAppointmentWithSlotsAsync saved. appointmentId={appointment.Id}, scheduledUtc={appointment.ScheduledTime:O}",
                    string.Empty,
                    "doctor",
                    "AppointmentService.CreateAppointmentWithSlotsAsync",
                    Serilog.Events.LogEventLevel.Information);

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogWithContext(
                    $"CreateAppointmentWithSlotsAsync error: {ex.Message}",
                    string.Empty,
                    "doctor",
                    "AppointmentService.CreateAppointmentWithSlotsAsync",
                    Serilog.Events.LogEventLevel.Error);
                
                return false;
                // In production, you might want to log the exception here
            }
        }

        /// <summary>
        /// Check if patient exists by identity number
        /// </summary>
        // public async Task<bool> PatientExistsAsync(string identityNumber)
        // {
        //     return await _context.Patients.AnyAsync(p => p.IdentityNumber == identityNumber);
        // }
        public async Task<bool> CreateSingleSlotAppointmentAsync(SingleAppointmentCreateViewModel model)
        {
            try
            {
                var selectedSlots = (model.SelectedSlotIndices ?? new List<int>())
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                if (!selectedSlots.Any() && model.SlotIndex >= 0)
                {
                    selectedSlots.Add(model.SlotIndex);
                }

                if (!selectedSlots.Any() || selectedSlots.Count > 4)
                {
                    LogHelper.LogWithContext(
                        $"CreateSingleSlotAppointmentAsync invalid slot selection count. count={selectedSlots.Count}",
                        string.Empty,
                        "patient",
                        "AppointmentService.CreateSingleSlotAppointmentAsync",
                        Serilog.Events.LogEventLevel.Warning);

                    return false;
                }

                var isContiguous = selectedSlots.Zip(selectedSlots.Skip(1), (a, b) => b - a).All(diff => diff == 1);
                if (!isContiguous)
                {
                    LogHelper.LogWithContext(
                        $"CreateSingleSlotAppointmentAsync non-contiguous slots selected. slots={string.Join(',', selectedSlots)}",
                        string.Empty,
                        "patient",
                        "AppointmentService.CreateSingleSlotAppointmentAsync",
                        Serilog.Events.LogEventLevel.Warning);

                    return false;
                }

                var startSlotIndex = selectedSlots.Min();
                var endSlotIndex = selectedSlots.Max();

                LogHelper.LogWithContext(
                    $"CreateSingleSlotAppointmentAsync start. patientId={model.PatientId}, doctorId={model.DoctorId}, slots={startSlotIndex}-{endSlotIndex}, rawUtc='{model.Time}'",
                    string.Empty,
                    "patient",
                    "AppointmentService.CreateSingleSlotAppointmentAsync",
                    Serilog.Events.LogEventLevel.Information);

                string? prescriptionFileName = null;

                // Handle prescription file upload if provided
                if (model.PrescriptionFile != null && model.PrescriptionFile.Length > 0)
                {
                    var (isValid, errorMessage) = _fileService.ValidatePrescriptionFile(model.PrescriptionFile);
                    if (!isValid)
                        throw new ArgumentException(errorMessage);

                    prescriptionFileName = await _fileService.SavePrescriptionFileAsync(model.PrescriptionFile);
                    if (string.IsNullOrEmpty(prescriptionFileName))
                        throw new Exception(_locService.GetSystem("Error_SavePrescriptionFailed"));
                }

                // Expect client to send UTC ISO in model.Time. Reject if missing/invalid.
                if (string.IsNullOrWhiteSpace(model.Time) || !DateTimeOffset.TryParse(model.Time, out var dto))
                {
                    LogHelper.LogWithContext(
                        $"CreateSingleSlotAppointmentAsync invalid UTC input. rawUtc='{model.Time}'",
                        string.Empty,
                        "patient",
                        "AppointmentService.CreateSingleSlotAppointmentAsync",
                        Serilog.Events.LogEventLevel.Error);

                    return false; // invalid request from client — require UTC time
                }

                var appointmentStartUtc = dto.UtcDateTime;
                LogHelper.LogWithContext(
                    $"CreateSingleSlotAppointmentAsync parsed UTC. utc={appointmentStartUtc:o}",
                    string.Empty,
                    "patient",
                    "AppointmentService.CreateSingleSlotAppointmentAsync",
                    Serilog.Events.LogEventLevel.Debug);

                var appointment = new Appointment
                {
                    PatientId = model.PatientId,
                    DoctorId = model.DoctorId,
                    ScheduledTime = appointmentStartUtc,
                    Status = AppointmentStatus.Pending,
                    Notes = model.Notes ?? string.Empty,
                    StartSlotIndex = startSlotIndex,
                    EndSlotIndex = endSlotIndex,
                    PrescriptionFileName = prescriptionFileName
                };

                await _dbSet.AddAsync(appointment);
                await _context.SaveChangesAsync();

                LogHelper.LogWithContext(
                    $"CreateSingleSlotAppointmentAsync saved. appointmentId={appointment.Id}, scheduledUtc={appointment.ScheduledTime:o}, status={appointment.Status}, slots={appointment.StartSlotIndex}-{appointment.EndSlotIndex}",
                    string.Empty,
                    "patient",
                    "AppointmentService.CreateSingleSlotAppointmentAsync",
                    Serilog.Events.LogEventLevel.Information);
                return true;
            }
            catch
            {
                LogHelper.LogWithContext(
                    $"CreateSingleSlotAppointmentAsync failed for patientId={model.PatientId}, doctorId={model.DoctorId}, slotIndex={model.SlotIndex}, rawUtc='{model.Time}'",
                    string.Empty,
                    "patient",
                    "AppointmentService.CreateSingleSlotAppointmentAsync",
                    Serilog.Events.LogEventLevel.Error);

                return false;
            }
        }
        public async Task<bool> IsSlotAvailableAsync(DateTime scheduledDate, int doctorId, int slotIndex)
        {
            // تأكد أن التاريخ يعامل كيوم فقط (بدون وقت)
            var day = scheduledDate.Date;
            LogHelper.LogWithContext(
                $"IsSlotAvailableAsync start. doctorId={doctorId}, day={day:yyyy-MM-dd}, slotIndex={slotIndex}",
                string.Empty,
                "patient",
                "AppointmentService.IsSlotAvailableAsync",
                Serilog.Events.LogEventLevel.Debug);

            var isAvailable = !await _context.Appointments.AnyAsync(a =>
                a.DoctorId == doctorId &&
                a.ScheduledTime.Date == day &&
                a.StartSlotIndex <= slotIndex &&
                a.EndSlotIndex >= slotIndex &&
                a.Status == AppointmentStatus.Confirmed // أو أي حالة لا تعتبر حجز مؤكد
            );

            LogHelper.LogWithContext(
                $"IsSlotAvailableAsync result. doctorId={doctorId}, day={day:yyyy-MM-dd}, slotIndex={slotIndex}, isAvailable={isAvailable}",
                string.Empty,
                "patient",
                "AppointmentService.IsSlotAvailableAsync",
                Serilog.Events.LogEventLevel.Debug);

            return isAvailable;
        }
        public async Task CancelOtherPendingAppointmentsAsync(int approvedAppointmentId)
        {
            var approvedAppointment = await _dbSet.FirstOrDefaultAsync(a => a.Id == approvedAppointmentId);
            if (approvedAppointment == null) return;

            var toCancel = await _dbSet
                .Where(a =>
                    a.Id != approvedAppointmentId &&
                    a.DoctorId == approvedAppointment.DoctorId &&
                    a.ScheduledTime == approvedAppointment.ScheduledTime &&
                    a.Status == AppointmentStatus.Pending)
                .ToListAsync();

            if (toCancel.Any())
            {
                foreach (var appt in toCancel)
                {
                    appt.Status = AppointmentStatus.Cancelled;
                }
                _dbSet.UpdateRange(toCancel);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Update appointment with new prescription file
        /// </summary>
        public async Task<bool> UpdateAppointmentWithPrescriptionAsync(int appointmentId, SingleAppointmentCreateViewModel model)
        {
            try
            {
                var appointment = await _dbSet.FirstOrDefaultAsync(a => a.Id == appointmentId);
                if (appointment == null)
                    return false;

                // Delete old prescription file if exists
                if (!string.IsNullOrEmpty(appointment.PrescriptionFileName))
                {
                    await _fileService.DeletePrescriptionFileAsync(appointment.PrescriptionFileName);
                }

                // Save new prescription file if provided
                string? newPrescriptionFileName = null;
                if (model.PrescriptionFile != null && model.PrescriptionFile.Length > 0)
                {
                    var (isValid, _) = _fileService.ValidatePrescriptionFile(model.PrescriptionFile);
                    if (!isValid)
                        return false;

                    newPrescriptionFileName = await _fileService.SavePrescriptionFileAsync(model.PrescriptionFile);
                    if (string.IsNullOrEmpty(newPrescriptionFileName))
                        return false;
                }

                // Update appointment
                appointment.PrescriptionFileName = newPrescriptionFileName;
                appointment.Notes = model.Notes ?? appointment.Notes;
                
                _dbSet.Update(appointment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Delete appointment and its prescription file
        /// </summary>
        public async Task<bool> DeleteAppointmentWithFilesAsync(int id)
        {
            try
            {
                var appointment = await _dbSet.FirstOrDefaultAsync(a => a.Id == id);
                if (appointment == null)
                    return false;

                // Delete prescription file if exists
                if (!string.IsNullOrEmpty(appointment.PrescriptionFileName))
                {
                    await _fileService.DeletePrescriptionFileAsync(appointment.PrescriptionFileName);
                }

                // Delete appointment
                _dbSet.Remove(appointment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
  public async Task<List<Patient>> SearchPatientsAsync(string term)
{
    if (string.IsNullOrWhiteSpace(term)) return new List<Patient>();

    return await _context.Patients
        .Include(p => p.User) // نضمن تحميل بيانات المستخدم
        .Where(p => p.User.FirstName.Contains(term) || 
                    p.User.LastName.Contains(term) 
                    )
        .Take(5)
        .ToListAsync();
}

    }

    
}


