using Microsoft.AspNetCore.Identity;

namespace IPTS.Models.Entites
{
    public class UserType
    {
        public int Id { get; set; }
        public string Name { get; set; } // مثل: "Admin", "Customer", "Vendor" 
        public bool HasDashboard { get; set; }
        public bool Registerable { get; set; }
        public string? DefaultAction{ get; set; }
        public string? DefaultController{ get; set; }
        public string? DefaultArea{ get; set; }
        public string? DefaultRoleId{ get; set; }
        public bool RequireAdminApproval { get; set; } = false;
        public virtual ICollection<AppUser> Users { get; set; } = [];
        public virtual IdentityRole? Role { get; set; } 
    }
}
