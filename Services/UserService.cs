using AutoMapper;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Areas.Doctor.ViewsModels;
using IPTS.Data;
using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IPTS.Services
{
    public class UserService(IMapper mapper,EmailService emailService, ApplicationDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration) : BaseService<AppUser>(context, mapper)
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly EmailService _emailService = emailService;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly IConfiguration _configuration = configuration;

        private async Task<AppUser> CreateUserAndSetTheDefaultRoleAsync(AppUser user, string password, string userTypeName)
        {
            await ValidateEmailAndPhoneAsync(user.Email, user.PhoneNumber);

            var userType = await _context.UserTypes.FirstOrDefaultAsync(ut => ut.Name == userTypeName)
                ?? throw new Exception("User type not found.");

            user.UserType = userType;

            var defaultRoleId = userType.DefaultRoleId ?? throw new Exception("Default role id is not set for this user type.");
            var defaultRole = await _roleManager.FindByIdAsync(defaultRoleId.ToString())
                ?? throw new Exception("Default role not found.");

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
                throw new Exception($"User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            var roleResult = await _userManager.AddToRoleAsync(user, defaultRole.Name);

            if (!roleResult.Succeeded)
                throw new Exception($"Adding role failed: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");

            return user;
        }
        //public async Task CreateAsync(CreateAdminViewModel model)
        //{
        //    var user = new AppUser
        //    {
        //        UserName = model.UserName,
        //        Email = model.Email,
        //        PhoneNumber = model.PhoneNumber,
        //        Status = model.Status
        //    };
        //    user = await CreateUserAndSetTheDefaultRoleAsync(user, model.Password,  model.UserTypeName);

        //    await _context.Admins.AddAsync(new Admin() { UserId = user.Id });

        //    await _context.SaveChangesAsync();
        //}
        public async Task CreateAsync(UserFormViewModel model, string UserTypeName)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new AppUser
                {
                    UserName = model.UserName,
                    LastName=model.LastName,
                    FirstName=model.FirstName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Status = EnUserStatus.Active
                };

                user = await CreateUserAndSetTheDefaultRoleAsync(user, model.Password, UserTypeName);

                switch (UserTypeName.ToLower())
                {
                    case "admin":
                        await _context.Admins.AddAsync(new Admin { UserId = user.Id });
                        break;

             
                    case "doctor":
                        if (model.Doctor == null)
                            throw new Exception("Doctor information is required.");

                        await _context.Doctors.AddAsync(new Doctor
                        {
                            UserId = user.Id,
                            SpecialtyId = model.Doctor.SpecialtyId,
                        });
                        break;

                    case "patient":
                        if (model.Patient == null)
                            throw new Exception("Patient information is required.");
                        await _context.Patients.AddAsync(new Patient
                        {
                            UserId = user.Id,
                            IdentityNumber = model.Patient.IdentityNumber,
                            BirthDate = model.Patient.BirthDate.ToUniversalTime()
                        });
                        break;

                    default:
                        throw new Exception("Unsupported user type.");
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task UpdateAsync(UserFormViewModel model, string userType)
        {
                await UpdateEntityAsync(model.Id, model);
        }

        public async Task UpdateProfileAsync(UserProfileViewModel model)
        {
            await UpdateEntityAsync(model.Id, model);
        }
        private async Task UpdateEntityAsync<TViewModel>(string userId, TViewModel model)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users
                    .Include(u => u.Admin)
                    .Include(u => u.Doctor)
                    .Include(u => u.Patient)
                    .FirstOrDefaultAsync(u => u.Id == userId)
                    ?? throw new Exception("User not found");

                // التحقق من البريد والهاتف
                var emailProp = typeof(TViewModel).GetProperty("Email");
                var phoneProp = typeof(TViewModel).GetProperty("PhoneNumber");

                var email = emailProp?.GetValue(model)?.ToString() ?? "";
                var phoneNumber = phoneProp?.GetValue(model)?.ToString() ?? "";

                await ValidateEmailAndPhoneAsync(email, phoneNumber, userId);

                if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    user.EmailConfirmed = false;
                }

                _mapper.Map(model, user);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<IdentityResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });

            return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        }
        private async Task ValidateEmailAndPhoneAsync(string email, string phoneNumber, string? userId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new Exception("Email cannot be empty.");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new Exception("Phone number cannot be empty.");

            var existingUserByEmail = await _userManager.FindByEmailAsync(email);
            if (existingUserByEmail != null && existingUserByEmail.Id != userId)
                throw new Exception("This email is already in use.");

            var existingUserByPhone = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.Id != userId);
            if (existingUserByPhone != null)
                throw new Exception("This phone number is already in use.");
        }
        public async Task<IdentityResult> RegisterAsync(RegisterViewModel model)
        {
            var userType = await _context.UserTypes.FirstOrDefaultAsync(ut => ut.Name == model.UserTypeName && ut.Registerable) ?? throw new Exception("This user type is not allowed for registration.");
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userStatus = userType.RequireAdminApproval ? EnUserStatus.Pending : EnUserStatus.Active;

                var user = new AppUser
                {
                    UserName = model.UserName,
                    LastName=model.LastName,
                    FirstName=model.FirstName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Status = userStatus
                };

                user = await CreateUserAndSetTheDefaultRoleAsync(user, model.Password, model.UserTypeName);

                switch (model.UserTypeName.ToLower())
                {
                    case "admin":
                        await _context.Admins.AddAsync(new Admin { UserId = user.Id });
                        break;

                    case "doctor":
                        if (model.Doctor == null)
                            throw new Exception("Doctor information is required.");
                        await _context.Doctors.AddAsync(new Doctor
                        {
                            UserId = user.Id,
                            SpecialtyId = model.Doctor.SpecialtyId,
                        });
                        break;

                    case "patient":
                        if (model.Patient == null)
                            throw new Exception("Patient information is required.");
                        await _context.Patients.AddAsync(new Patient
                        {
                            UserId = user.Id,
                            IdentityNumber = model.Patient.IdentityNumber,
                            BirthDate = model.Patient.BirthDate.ToUniversalTime()
                        });
                        break;

                    default:
                        throw new Exception("Unsupported user type.");
                }

                await _context.SaveChangesAsync();
                var baseUrl = _configuration["App:BaseUrl"]; 
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user); // should be the dinamic bese url
                var confirmationLink = $"{baseUrl}/Auth/ConfirmEmail?userId={user.Id}&token={Uri.EscapeDataString(emailToken)}";

                await _emailService.SendEmail(
                    user.Email,
                    "Confirm your email",
                    $"<p>Welcome!</p><p>Please confirm your email by clicking the link below:</p><p><a href='{confirmationLink}'>Confirm Email</a></p>"
                );
                await transaction.CommitAsync();

                return IdentityResult.Success;
            }
            catch(Exception err)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "Registeration Failed",
                    Description = err.Message
                });
            }
        }

    public async Task RegisterPatientFromDoctorAsync(PatientRegistrationViewModel model)
{
    // 1. التحققات المعتادة
    if (await _userManager.FindByNameAsync(model.UserName) != null)
        throw new Exception("This username is already taken.");

    if (await _context.Patients.AnyAsync(p => p.IdentityNumber == model.NationalId))
        throw new Exception("This National ID is already registered.");

    await using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // 2. تجهيز كلمة المرور حسب نمطك المقترح: Aa + IdentityNumber + _1
        // مثال: Aa1022334455_1
        string generatedPassword = $"Aa{model.NationalId}_1";

        var user = new AppUser
        {
            UserName = model.UserName,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            Status = EnUserStatus.Active,
            EmailConfirmed = false
        };

        // 3. استدعاء الدالة باستخدام كلمة المرور الجديدة
        user = await CreateUserAndSetTheDefaultRoleAsync(user, generatedPassword, "patient");

        // 4. ربط سجل المريض
        var patient = new Patient
        {
            UserId = user.Id,
            IdentityNumber = model.NationalId,
            BirthDate = model.DateOfBirth.ToUniversalTime()
        };

        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
    }
    catch (Exception)
    {
        await transaction.RollbackAsync();
        throw;
    }
}

    }

    
}
