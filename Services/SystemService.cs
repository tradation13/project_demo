using IPTS.Models.Entites;
using IPTS.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;

namespace IPTS.Services
{
    public class SystemService(IDbConnection dbConnection, UserManager<AppUser> userManager)
    {
        private readonly IDbConnection _dbConnection = dbConnection;
        private readonly UserManager<AppUser> _userManager = userManager;

        public async Task<(bool HasDashboard, List<LogViewModel> Logs)> GetLogsAsync(string userId, string? systemSection = null)
        {
            var user = await _userManager.Users.Include(u => u.UserType)
                                               .FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.UserType == null)
                return (false, new List<LogViewModel>());

            var hasDashboard = user.UserType.HasDashboard;
            var logs = new List<LogViewModel>();

            if (_dbConnection.State != ConnectionState.Open)
                _dbConnection.Open();

            var query = "SELECT level, message_template, timestamp, log_event FROM logs ORDER BY timestamp DESC LIMIT 200";
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

                    // فلترة حسب systemSection إذا تم تمريره
                    if (!string.IsNullOrWhiteSpace(systemSection) &&
                        !systemSectionValue.Contains(systemSection, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    logs.Add(new LogViewModel
                    {
                        Level = reader["Level"]?.ToString() ?? "",
                        Description = reader["message_template"]?.ToString() ?? "",
                        Timestamp = Convert.ToDateTime(reader["timestamp"]),
                        UserId = userIdFromEvent,
                        UserRole = userRole,
                        SystemSection = systemSectionValue,
                    });
                }
            }

            return (hasDashboard, logs);
        }
    }
}
