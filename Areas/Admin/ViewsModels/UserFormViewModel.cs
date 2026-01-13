using IPTS.Models.Enums;
using IPTS.Areas.Admin.ViewsModels;
using System.ComponentModel.DataAnnotations;

namespace IPTS.Areas.Admin.ViewsModels
{

    public class UserFormViewModel
    {
        public string? Id { get; set; }
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        //public string UserTypeName { get; set; }
        public EnUserStatus? Status { get; set; } = EnUserStatus.Active;
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        
        public AdminFormViewModel? Admin {  get; set; }
        public CustomerFormViewModel? Customer { get; set; }
        public PatientFormViewModel? Patient { get; set; }   // جديد
        public DoctorFormViewModel? Doctor { get; set; }     // جديد
    }
}
