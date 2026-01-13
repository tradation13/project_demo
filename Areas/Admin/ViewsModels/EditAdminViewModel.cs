using IPTS.Models.Enums;

namespace IPTS.Areas.Admin.ViewsModels
{
    public class EditAdminViewModel
    {
        public string Id { get; set; }

        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int? UserTypeId { get; set; }
        public EnUserStatus? Status { get; set; } = EnUserStatus.Active;
    }
}
