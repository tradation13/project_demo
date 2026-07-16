using IPTS.ViewModels;

namespace IPTS.Areas.Admin.ViewsModels
{
    public class AdminLogsPageViewModel
    {
        public string Tab { get; set; } = "logging";

        // Logging filters
        public string? Section { get; set; }
        public string? Level { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        // Auditing filters
        public int? Action { get; set; }
        public string? Actor { get; set; }

        public List<LogViewModel> Logs { get; set; } = [];
        public List<AuditLogViewModel> Audits { get; set; } = [];
    }
}
