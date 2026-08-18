using IPTS.Models.Enums;

namespace IPTS.ViewModels
{
    public class UserListViewModel
    {
        public string Id { get; set; } = string.Empty;
        public int? PatientId { get; set; }
        public string UserTypeName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty ;
        public DateTime CreatedAt { get; set; }
        public EnUserStatus Status { get; set; }  // enum بدل int
    }
}
