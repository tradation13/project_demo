using IPTS.Models.Enums;

namespace IPTS.Models.Entites
{
    public class AuditLog
    {
        public int Id { get; set; }

        /// <summary>قيمة EnAuditAction كـ int (بدون جدول Enum منفصل).</summary>
        public int Action { get; set; }

        public string? ActorUserId { get; set; }
        public string? ActorUserName { get; set; }
        public string? TargetUserId { get; set; }
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string Details { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public EnAuditAction ActionEnum => (EnAuditAction)Action;
    }
}
