using AutoMapper;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Areas.Doctor.ViewsModels;
using IPTS.Data;
using IPTS.Helpers;
using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.Resources;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IPTS.Services
{
    public class UserService(LocService locService,IHttpContextAccessor httpContextAccessor,LinkGenerator linkGenerator,HttpUser currentUser, IMapper mapper,EmailService emailService, ApplicationDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration) : BaseService<AppUser>(context, mapper)
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly EmailService _emailService = emailService;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly IConfiguration _configuration = configuration;
        private readonly LocService _locService = locService;
        private readonly HttpUser _currentUser = currentUser;
        private readonly LinkGenerator _linkGenerator = linkGenerator; 
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor; 
        

        private async Task<AppUser> CreateUserAndSetTheDefaultRoleAsync(AppUser user, string password, string userTypeName)
        {
            await ValidateEmailAndPhoneAsync(user.Email, user.PhoneNumber);

           var userType = await _context.UserTypes.FirstOrDefaultAsync(ut => ut.Name == userTypeName)
               ?? throw new Exception(_locService.GetSystem("Error_UserTypeNotFound"));

            user.UserType = userType;

           var defaultRoleId = userType.DefaultRoleId 
                   ?? throw new Exception(_locService.GetSystem("Error_DefaultRoleNotSet"));
          var defaultRole = await _roleManager.FindByIdAsync(defaultRoleId.ToString())
                 ?? throw new Exception(_locService.GetSystem("Error_DefaultRoleNotFound"));

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
{
    var errorDetails = string.Join(", ", result.Errors.Select(e => e.Description));
    throw new Exception(string.Format(_locService.GetSystem("Error_UserCreationFailed"), errorDetails));
}

            var roleResult = await _userManager.AddToRoleAsync(user, defaultRole.Name);

           if (!roleResult.Succeeded)
{
    var errorDetails = string.Join(", ", roleResult.Errors.Select(e => e.Description));
    throw new Exception(string.Format(_locService.GetSystem("Error_AddingRoleFailed"), errorDetails));
}

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
    throw new Exception(_locService.GetSystem("Error_DoctorRequired"));

                        await _context.Doctors.AddAsync(new Doctor
                        {
                            UserId = user.Id,
                            SpecialtyId = model.Doctor.SpecialtyId,
                        });
                        break;

                    case "patient":
                       if (model.Patient == null)
    throw new Exception(_locService.GetSystem("Error_PatientRequired"));
                        await _context.Patients.AddAsync(new Patient
                        {
                            UserId = user.Id,
                            IdentityNumber = model.Patient.IdentityNumber,
                            BirthDate = model.Patient.BirthDate.ToUniversalTime()
                        });
                        break;

                   default:
    throw new Exception(_locService.GetSystem("Error_UnsupportedUserType"));
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
                    ?? throw new Exception(_locService.GetSystem("Error_UserNotFound"));

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
                return IdentityResult.Failed(new IdentityError { Description = _locService.GetSystem("Error_UserNotFound") });

            return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        }
        private async Task ValidateEmailAndPhoneAsync(string email, string phoneNumber, string? userId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new Exception(_locService.GetSystem("Error_EmailEmpty"));

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new Exception(_locService.GetSystem("Error_PhoneEmpty"));

            var existingUserByEmail = await _userManager.FindByEmailAsync(email);
            if (existingUserByEmail != null && existingUserByEmail.Id != userId)
                throw new Exception(_locService.GetSystem("Error_EmailAlreadyInUse"));

            var existingUserByPhone = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.Id != userId);
            if (existingUserByPhone != null)
                throw new Exception(_locService.GetSystem("Error_PhoneAlreadyInUse"));
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
                            throw new Exception(_locService.GetSystem("Error_DoctorRequired"));
                        await _context.Doctors.AddAsync(new Doctor
                        {
                            UserId = user.Id,
                            SpecialtyId = model.Doctor.SpecialtyId,
                        });
                        break;

                    case "patient":
                        if (model.Patient == null)
                            throw new Exception(_locService.GetSystem("Error_PatientRequired"));
                        await _context.Patients.AddAsync(new Patient
                        {
                            UserId = user.Id,
                            IdentityNumber = model.Patient.IdentityNumber,
                            BirthDate = model.Patient.BirthDate.ToUniversalTime()
                        });
                        break;

                    default:
    throw new Exception(_locService.GetSystem("Error_UnsupportedUserType"));
                }

                await _context.SaveChangesAsync();

               
var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

var confirmationLink = _linkGenerator.GetUriByAction(
    _httpContextAccessor.HttpContext!,
    action: "ConfirmEmail", 
    controller: "Auth",
    values: new { userId = user.Id, token = emailToken }
);

              await _emailService.SendEmail(
    user.Email,
    _locService.GetSystem("Email_Subject_Confirm"),
    string.Format(_locService.GetSystem("Email_Body_Confirm"), confirmationLink)
);
                await transaction.CommitAsync();

                return IdentityResult.Success;
            }
            catch(Exception err)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = _locService.GetSystem("Code_RegistrationFailed"),
                    Description = err.Message
                });
            }
        }

        private async Task<string> GetUserFullNameByIdAsync(string userId)
{
    if (string.IsNullOrEmpty(userId)) return "System";

    var user = await _userManager.Users
        .Where(u => u.Id == userId)
        .Select(u => new { u.FirstName, u.LastName })
        .FirstOrDefaultAsync();

    return user != null ? $"{user.FirstName} {user.LastName}" : _locService.GetSystem("Label_UnknownUser");
}

public async Task RegisterPatientFromDoctorAsync(PatientRegistrationViewModel model)
{
    
    var doctorName = await GetUserFullNameByIdAsync(_currentUser.userId);

   
    if (await _userManager.FindByNameAsync(model.UserName) != null)
        throw new Exception(_locService.GetSystem("Error_UsernameTaken"));

    if (await _context.Patients.AnyAsync(p => p.IdentityNumber == model.NationalId))
        throw new Exception(_locService.GetSystem("Error_NationalIdRegistered"));

    string generatedPassword = $"Aa{model.NationalId}_1";
    AppUser? userForEmail = null;

  
    using (var transaction = await _context.Database.BeginTransactionAsync())
    {
        try
        {
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

           
            userForEmail = await CreateUserAndSetTheDefaultRoleAsync(user, generatedPassword, "patient");

           
            var patient = new Patient
            {
                UserId = userForEmail.Id,
                IdentityNumber = model.NationalId,
                BirthDate = model.DateOfBirth.ToUniversalTime()
            };

            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();

          
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
   
            await transaction.RollbackAsync();
           throw new Exception(string.Format(_locService.GetSystem("Error_PatientRegistrationFailed"), ex.Message));
        }
    } 

   
    if (userForEmail != null)
    {

        var baseUrl = _configuration["App:BaseUrl"];
        var loginUrl = $"{baseUrl}/Auth/Login";


        var emailSubject = _locService.GetSystem("Email_Welcome_Subject");
        
       var emailBody = string.Format(_locService.GetSystem("Email_Welcome_Body"), 
    model.FirstName, 
    model.LastName, 
    doctorName, 
    loginUrl, 
    model.UserName, 
    generatedPassword);
        await _emailService.SendEmail(userForEmail.Email, emailSubject, emailBody);
    }
}
    }

    
}
