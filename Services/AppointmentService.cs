using AutoMapper;
using IPTS.Data;
using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.Resources;
using IPTS.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IPTS.Services
{
    public class AppointmentService(LocService locService,EmailService emailService, ApplicationDbContext context, IMapper mapper, UserService userService) : BaseService<Appointment>(context, mapper)
    {
        private readonly LocService _locService = locService;
        private readonly UserService _userService = userService;
        private readonly EmailService _emailService = emailService;
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
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
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
            var subject = _locService.GetSystem("Email_Subject_AppointmentRejected");
           var body = string.Format(
    _locService.GetSystem("Email_Body_AppointmentRejected"), 
    patientName, 
    reason
);
            await _emailService.SendEmail(toEmail, subject, body);
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
                    .ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.User)
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

        public async Task<Patient?> SearchPatientByIdentityNumberAsync(string identityNumber)
        {
            return await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.IdentityNumber == identityNumber);
        }


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
    string doctorTimeZoneId = "Europe/Berlin", int leadMinutes = 0)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(doctorTimeZoneId);

            // تاريخ اليوم المطلوب كـ "وقت حائطي" (بدون Kind) ثم ساعات العمل
            var dateWallClock = DateTime.SpecifyKind(dateLocal.Date, DateTimeKind.Unspecified);
            var workStartLocal = dateWallClock.AddHours(8);   // 08:00
            var workEndLocal = dateWallClock.AddHours(18);  // 18:00

            // نافذة اليوم بالـ UTC
            var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(workStartLocal, tz);
            var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(workEndLocal, tz);

            // الآن الحالي في منطقة الطبيب
            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

            // اجلب مواعيد اليوم دفعة واحدة (الممنوعة من الحجز)
            var activeStatuses = new[] { AppointmentStatus.Confirmed /*, AppointmentStatus.Pending*/ };

            var todaysAppts = await _dbSet
                .AsNoTracking()
                .Where(a => a.DoctorId == doctorId
                            && activeStatuses.Contains(a.Status)
                            && a.ScheduledTime >= dayStartUtc
                            && a.ScheduledTime < dayEndUtc)
                .Select(a => new { a.StartSlotIndex, a.EndSlotIndex })
                .ToListAsync();

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

                result.Add(new AppointmentTimeSlotViewModel
                {
                    Time = timeLocal.ToString("HH:mm"),
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

                // Calculate total duration based on selected slots
                var totalDuration = selectedSlotIndices.Count * 20; // 20 minutes per slot
                
                // Use the start time (first slot) as the appointment start time
                var startSlotIndex = selectedSlotIndices.Min();
                var appointmentStartTime = DateTime.SpecifyKind(
                    model.ScheduledDate.AddMinutes(startSlotIndex * 20), 
                    DateTimeKind.Utc
                );

                // Create ONE appointment with the total duration
                var appointment = new Appointment
                {
                    PatientId = model.PatientId,
                    DoctorId = model.DoctorId,
                    ScheduledTime = appointmentStartTime, // Start time of the first selected slot
                    Status = AppointmentStatus.Confirmed,
                    Notes = string.IsNullOrWhiteSpace(model.Notes) ? string.Empty : model.Notes,
                    StartSlotIndex = startSlotIndex,
                    EndSlotIndex = selectedSlotIndices.Max()
                };

                await _dbSet.AddAsync(appointment);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch
            {
                return false;
                // In production, you might want to log the exception here
            }
        }

        /// <summary>
        /// Check if patient exists by identity number
        /// </summary>
        public async Task<bool> PatientExistsAsync(string identityNumber)
        {
            return await _context.Patients.AnyAsync(p => p.IdentityNumber == identityNumber);
        }
        public async Task<bool> CreateSingleSlotAppointmentAsync(SingleAppointmentCreateViewModel model)
        {
            try
            {
                // وقت بداية الخانة
                var startTimeUtc = DateTime.SpecifyKind(
                    model.ScheduledDate.AddMinutes(model.SlotIndex * 20),
                    DateTimeKind.Utc
                );

                var appointment = new Appointment
                {
                    PatientId = model.PatientId,
                    DoctorId = model.DoctorId,
                    ScheduledTime = startTimeUtc,
                    Status = AppointmentStatus.Pending,
                    Notes = model.Notes ?? string.Empty,
                    StartSlotIndex = model.SlotIndex,
                    EndSlotIndex = model.SlotIndex // نفس الخانة
                };

                await _dbSet.AddAsync(appointment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> IsSlotAvailableAsync(DateTime scheduledDate, int doctorId, int slotIndex)
        {
            // تأكد أن التاريخ يعامل كيوم فقط (بدون وقت)
            var day = scheduledDate.Date;

            return !await _context.Appointments.AnyAsync(a =>
                a.DoctorId == doctorId &&
                a.ScheduledTime.Date == day &&
                a.StartSlotIndex <= slotIndex &&
                a.EndSlotIndex >= slotIndex &&
                a.Status == AppointmentStatus.Confirmed // أو أي حالة لا تعتبر حجز مؤكد
            );
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
    }
}
