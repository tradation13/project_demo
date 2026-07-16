using IPTS.Models.Entites;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;

namespace IPTS.Services
{
    public class SystemService(IDbConnection dbConnection, UserManager<AppUser> userManager)
    {
        private readonly IDbConnection _dbConnection = dbConnection;
        private readonly UserManager<AppUser> _userManager = userManager;

        public async Task<(bool HasDashboard, List<LogViewModel> Logs)> GetLogsAsync(
            string userId,
            string? systemSection = null,
            string? level = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            var user = await _userManager.Users.Include(u => u.UserType)
                                               .FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.UserType == null)
                return (false, new List<LogViewModel>());

            var hasDashboard = user.UserType.HasDashboard;
            var logs = new List<LogViewModel>();

            if (_dbConnection.State != ConnectionState.Open)
                _dbConnection.Open();

            var query = "SELECT level, message_template, timestamp, log_event FROM logs ORDER BY timestamp DESC LIMIT 500";
            using (var cmd = _dbConnection.CreateCommand())
            {
                cmd.CommandText = query;
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string userIdFromEvent = "";
                    string userRole = "";
                    string systemSectionValue = "";

                    string logEvent = reader["log_event"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(logEvent))
                    {
                        try
                        {
                            var json = JsonDocument.Parse(logEvent);
                            var root = json.RootElement;
                            if (root.TryGetProperty("Properties", out var props))
                            {
                                userIdFromEvent = props.TryGetProperty("UserId", out var uid) ? uid.GetString() ?? "" : "";
                                userRole = props.TryGetProperty("UserRole", out var ur) ? ur.GetString() ?? "" : "";
                                systemSectionValue = props.TryGetProperty("SystemSection", out var ss) ? ss.GetString() ?? "" : "";
                            }
                        }
                        catch { }
                    }

                    var timestamp = Convert.ToDateTime(reader["timestamp"]);
                    var levelValue = reader["Level"]?.ToString() ?? reader["level"]?.ToString() ?? "";

                    if (!string.IsNullOrWhiteSpace(systemSection) &&
                        !systemSectionValue.Contains(systemSection, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(level) &&
                        !string.Equals(levelValue, level, StringComparison.OrdinalIgnoreCase) &&
                        !MatchesLevelAlias(levelValue, level))
                    {
                        continue;
                    }

                    if (from.HasValue && timestamp < from.Value)
                        continue;

                    if (to.HasValue && timestamp >= to.Value.Date.AddDays(1))
                        continue;

                    logs.Add(new LogViewModel
                    {
                        Level = NormalizeLevel(levelValue),
                        Description = reader["message_template"]?.ToString() ?? "",
                        Timestamp = timestamp,
                        UserId = userIdFromEvent,
                        UserRole = userRole,
                        SystemSection = systemSectionValue,
                    });

                    if (logs.Count >= 200)
                        break;
                }
            }

            return (hasDashboard, logs);
        }

        private static bool MatchesLevelAlias(string storedLevel, string filter)
        {
            var normalized = NormalizeLevel(storedLevel);
            return string.Equals(normalized, filter, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeLevel(string level)
        {
            return level switch
            {
                "0" or "Verbose" => "Verbose",
                "1" or "Debug" => "Debug",
                "2" or "Information" => "Information",
                "3" or "Warning" => "Warning",
                "4" or "Error" => "Error",
                "5" or "Fatal" => "Fatal",
                _ => string.IsNullOrWhiteSpace(level) ? "Unknown" : level
            };
        }
    }
}
