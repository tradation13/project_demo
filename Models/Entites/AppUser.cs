using IPTS.Models.Enums;
using IPTS.Models.Entites;
using Microsoft.AspNetCore.Identity;

namespace IPTS.Models.Entites
{
    public class AppUser : IdentityUser
    {
        public int? UserTypeId { get; set; }
        public UserType? UserType { get; set; }
        public EnUserStatus? Status { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Admin? Admin { get; set; }
        public Doctor? Doctor { get; set; }
        public Patient? Patient { get; set; }

    }
}
