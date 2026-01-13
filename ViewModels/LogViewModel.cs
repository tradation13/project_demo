namespace IPTS.ViewModels
{
    public class LogViewModel
    {
        public string Level { get; set; } = "";
        public string Description { get; set; } = "";
        public string UserId { get; set; } = "";
        public string UserRole { get; set; } = "";
        public string SystemSection { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string? UserName { get; set; } // لعرض اسم المستخدم
    }


}
