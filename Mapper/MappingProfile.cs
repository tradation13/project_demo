using AutoMapper;
using IPTS.Models.Entites;
using IPTS.ViewModels;
using IPTS.Areas.Admin.ViewsModels;
using IPTS.Models.Enums;

namespace IPTS.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // -----------------------------
            // User -> List / Profile ViewModels
            // -----------------------------

            CreateMap<AppUser, UserListViewModel>()
                .ForMember(dest => dest.UserTypeName, opt =>
                    opt.MapFrom(src => src.UserType != null ? src.UserType.Name : "-"))
                .ForMember(dest => dest.PatientId, opt =>
                    opt.MapFrom(src => src.Patient != null ? (int?)src.Patient.Id : null))
                .ReverseMap();

            CreateMap<AppUser, UserProfileViewModel>()
                .ForMember(dest => dest.Admin, opt => opt.MapFrom(src => src.Admin))
                .ForMember(dest => dest.Doctor, opt => opt.MapFrom(src => src.Doctor))
                .ForMember(dest => dest.Patient, opt => opt.MapFrom(src => src.Patient))
                .ReverseMap()
                .ForMember(dest => dest.UserName, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore());

            CreateMap<UserProfileViewModel, AppUser>()
                .ForMember(dest => dest.UserType, opt => opt.Ignore())
                .ForMember(dest => dest.UserTypeId, opt => opt.Ignore())
                .ForMember(dest => dest.UserName, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
                .AfterMap((src, dest) =>
                {
                    if (!string.IsNullOrEmpty(src.Email))
                        dest.NormalizedEmail = src.Email.ToUpper();
                });

            // -----------------------------
            // Admin Mapping
            // -----------------------------

            CreateMap<Admin, AdminProfileViewModel>().ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            CreateMap<Admin, AdminFormViewModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ReverseMap()
                // المفاتيح لا تُحدَّث على الكيان المتتبَّع أثناء التعديل
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore());

            // -----------------------------
            // Doctor Mapping
            // -----------------------------

            CreateMap<Doctor, DoctorProfileViewModel>().ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src => src.PhotoUrl));
                
            CreateMap<Doctor, DoctorFormViewModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ReverseMap()
                // المفاتيح لا تُحدَّث على الكيان المتتبَّع أثناء التعديل
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore());

            // -----------------------------
            // Patient Mapping
            // -----------------------------

            CreateMap<Patient, PatientProfileViewModel>().ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src =>
                    src.BirthDate.HasValue
                        ? DateTime.SpecifyKind(src.BirthDate.Value, DateTimeKind.Utc)
                        : (DateTime?)null));
                // .ForMember(dest => dest.IdentityNumber, opt => opt.MapFrom(src => src.IdentityNumber));

            CreateMap<Patient, PatientFormViewModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                // .ForMember(dest => dest.IdentityNumber, opt => opt.MapFrom(src => src.IdentityNumber))
                .ReverseMap()
                // المفاتيح لا تُحدَّث على الكيان المتتبَّع أثناء التعديل (Id مفتاح أساسي و UserId مفتاح أجنبي)
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                // PostgreSQL timestamptz يتطلب Kind=Utc؛ حقل التاريخ من النموذج يأتي بـ Kind=Unspecified
                .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => DateTime.SpecifyKind(src.BirthDate, DateTimeKind.Utc)));

            // -----------------------------
            // UserFormViewModel Mapping
            // -----------------------------

            CreateMap<AppUser, UserFormViewModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Admin, opt => opt.MapFrom(src => src.Admin))
                .ForMember(dest => dest.Doctor, opt => opt.MapFrom(src => src.Doctor))
                .ForMember(dest => dest.Patient, opt => opt.MapFrom(src => src.Patient))
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.ConfirmPassword, opt => opt.Ignore())
                .ReverseMap()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.UserName, opt => opt.Ignore())
                .ForMember(dest => dest.Admin, opt => opt.MapFrom(src => src.Admin))
                .ForMember(dest => dest.Doctor, opt => opt.MapFrom(src => src.Doctor))
                .ForMember(dest => dest.Patient, opt => opt.MapFrom(src => src.Patient))
                .ForMember(dest => dest.UserType, opt => opt.Ignore())
                .ForMember(dest => dest.UserTypeId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .AfterMap((src, dest) =>
                {
                    if (!string.IsNullOrEmpty(src.Email))
                        dest.NormalizedEmail = src.Email.ToUpper();
                });


            // Entity to ViewModel
            CreateMap<TestGroup, TestGroupViewModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

            // ViewModel to Entity
            CreateMap<TestGroupViewModel, TestGroup>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Tests, opt => opt.Ignore()); // Tests are managed separately


            // Entity to ViewModel
            CreateMap<Test, TestViewModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.TestGroupId, opt => opt.MapFrom(src => src.TestGroupId))
                .ForMember(dest => dest.TestGroupName, opt => opt.MapFrom(src => src.TestGroup.Name));

            // ViewModel to Entity
            CreateMap<TestViewModel, Test>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.TestGroupId, opt => opt.MapFrom(src => src.TestGroupId))
                .ForMember(dest => dest.TestGroup, opt => opt.Ignore()); // Set via EF navigation

            // -----------------------------
            // Appointment Mapping
            // -----------------------------

            CreateMap<Appointment, AppointmentViewModel>()
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.PatientName, opt => 
                    opt.MapFrom(src => src.Patient.User != null ? 
                        $"{src.Patient.User.FirstName} {src.Patient.User.LastName}".Trim() : 
                        src.Patient.User.UserName ?? "Unknown"))
                // .ForMember(dest => dest.PatientIdentityNumber, opt => 
                //     opt.MapFrom(src => src.Patient.IdentityNumber ?? ""))
                .ForMember(dest => dest.PatientPhone, opt => 
                    opt.MapFrom(src => src.Patient.User.PhoneNumber ?? ""))
                .ForMember(dest => dest.PatientEmail, opt => 
                    opt.MapFrom(src => src.Patient.User.Email ?? ""))
                .ForMember(dest => dest.DoctorName, opt => 
                    opt.MapFrom(src => src.Doctor.User != null ? 
                        $"{src.Doctor.User.FirstName} {src.Doctor.User.LastName}".Trim() : 
                        src.Doctor.User.UserName ?? "Unknown"))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.StartSlotIndex, opt => 
                    opt.MapFrom(src => src.StartSlotIndex))
                .ForMember(dest => dest.EndSlotIndex, opt => 
                    opt.MapFrom(src => src.EndSlotIndex))
                .ReverseMap();

            CreateMap<Appointment, AppointmentCreateViewModel>().ReverseMap();
            CreateMap<Appointment, AppointmentEditViewModel>().ReverseMap();

            // -----------------------------
            // Appointment Mapping
            // -----------------------------
            CreateMap<AppUser, DoctorViewModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id.ToString()))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Doctor.Id))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()))
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src => src.Doctor != null ? src.Doctor.PhotoUrl : ""))
                .ForMember(dest => dest.Specialty, opt => opt.MapFrom(src => src.Doctor != null && src.Doctor.Specialty != null ? src.Doctor.Specialty.Name : ""))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src =>  0))
                .ForMember(dest => dest.YearsOfExperience, opt => opt.MapFrom(src =>  0))
                .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.Status == EnUserStatus.Active));
            // -----------------------------
            // Medical case Mapping
            // -----------------------------
            CreateMap<MedicalCase, MedicalCaseViewModel>().ReverseMap();
            // -----------------------------
            // Medical case Tests Mapping
            // -----------------------------
            CreateMap<MedicalCaseTest, MedicalCaseTestViewModel>().ReverseMap()
                .ForMember(dest => dest.Test, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt,
                    opt => opt.MapFrom(src =>
                        src.CreatedAt.Kind == DateTimeKind.Unspecified
                            ? DateTime.SpecifyKind(src.CreatedAt, DateTimeKind.Utc)
                            : src.CreatedAt.ToUniversalTime()
                    ));

            CreateMap<BlogPost, BlogPostViewModel>()
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images))
                .ForMember(dest => dest.MainImagePath, opt => opt.MapFrom(src => src.MainImagePath))
                .ReverseMap()
                .ForMember(dest => dest.Images, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
                .ForMember(dest => dest.MainImagePath, opt => opt.MapFrom(src => src.MainImagePath));

            CreateMap<BlogPostImage, BlogPostImageViewModel>()
                .ForMember(dest => dest.Url, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.FileName) ? string.Empty : $"/InternalStorage/BlogsImages/{src.FileName}"))
                .ReverseMap()
                .ForMember(dest => dest.BlogPost, opt => opt.Ignore());

        }
    }
}
