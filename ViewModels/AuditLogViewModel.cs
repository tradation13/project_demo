namespace IPTS.ViewModels
{
    public class AuditLogViewModel
    {
        public int Id { get; set; }
        public int Action { get; set; }
        public string ActionName { get; set; } = string.Empty;
        public string? ActorUserId { get; set; }
        public string? ActorUserName { get; set; }
        public string? TargetUserId { get; set; }
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string Details { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
