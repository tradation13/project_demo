using Serilog.Context;
using Serilog.Events;
using Serilog;

namespace IPTS.Helpers
{
    public static class LogHelper
    {
        public static void LogWithContext(string message, string userId, string userRole, string systemSection, LogEventLevel level = LogEventLevel.Information)
        {
            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("UserRole", userRole))
            using (LogContext.PushProperty("SystemSection", systemSection))
            {
                switch (level)
                {
                    case LogEventLevel.Fatal:
                        Log.Fatal(message);
                        break;
                    case LogEventLevel.Error:
                        Log.Error(message);
                        break;
                    case LogEventLevel.Warning:
                        Log.Warning(message);
                        break;
                    case LogEventLevel.Information:
                        Log.Information(message);
                        break;
                    case LogEventLevel.Debug:
                        Log.Debug(message);
                        break;
                    default:
                        Log.Information(message);
                        break;
                }
            }
        }
    }
}
