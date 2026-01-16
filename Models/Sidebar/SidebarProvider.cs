using IPTS.Helpers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Security.Claims;

namespace IPTS.Models.Sidebar
{
    public static class SidebarProvider
    {
        public static List<SidebarItemsGroup> GetSidebarItems(ClaimsPrincipal User)
        {    // Extract user roles from claims
            var Roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(cr => cr.Value);

            var AllElements = new List<SidebarItemsGroup>()
            {
                new()
                {
                    Title = "General",
                    Items =
                    [
                        new()
                        {
                            Title = "Dashboard", // later will make it dynamic - each account to its own dashboard -
                            Icon = "dashboard",
                            Action = "Index",
                            Controller = "Dashboard",
                            Area = Roles.First(), // Get it from Database based on account 
                            Roles = ["*"]
                        },
                        new()
                        {
                            Title = "Profile",
                            Icon = "user",
                            Action = "Index",
                            Controller = "Profile",
                            Area = "",
                            Roles = ["*"]
                        },
                         new()
                        {
                            Title = "Reset Password",
                            Icon = "lock",
                            Action = "ResetPassword",
                            Controller = "Auth",
                            Area = "",
                            Roles = ["*"]
                        }
                    ]
                },
                new()
                {
                    Title = "User Management",
                    Items =
                    [
                        new()
                        {
                            Title = "Users",
                            Icon = "users",
                            Action = "Index",
                            Controller = "Users",
                            Area = "admin",
                            Roles = ["admin"]
                        },
                        new()
                        {
                            Title = "Roles",
                            Icon = "user-shield",
                            Action = "Index",
                            Controller = "Roles",
                            Area = "admin",
                            Roles = ["admin"]
                        },
                        new()
                        {
                            Title = "Permissions",
                            Icon = "key",
                            Action = "Index",
                            Controller = "Permissions",
                            Area = "admin",
                            Roles = ["admin"]
                        },
                        new()
                        {
                            Title = "User Types",
                            Icon = "id-badge",
                            Action = "Index",
                            Controller = "UsersTypes",
                            Area = "admin",
                            Roles = ["admin"]
                        },
                        new()
                        {
                            Title = "Policies",
                            Icon = "clipboard-check",
                            Action = "Index",
                            Controller = "Policies",
                            Area = "admin",
                            Roles = ["admin"]
                        }
                    ],
                    Roles = ["*"]
                },
                new ()
                {
                    Title = "Tests Management",
                    Items = [
                        new ()
                        {
                            Title = "Test Groups",
                            Icon = "layer-group",
                            Action = "Index",
                            Controller = "TestGroups",
                            Area = "admin",
                            Roles = ["admin"]
                        },
                        new ()
                        {
                            Title = "Tests",
                            Icon = "flask",
                            Action = "Index",
                            Controller = "Tests",
                            Area = "admin",
                            Roles = ["admin"]
                        }
                    ],
                    Roles = ["admin"]
                },
                new() {
    Title = "Appointment Management",
    Items = [
        new ()
        {
            Title = "Appointments",
            Icon = "calendar-check",
            Action = "Index",
            Controller = "Appointments",
            Area = "doctor",
            Roles = ["doctor"]
        },
        new ()
        {
            Title = "Appointment Requests",
            Icon = "calendar-plus",
            Action = "Requests",
            Controller = "Appointments",
            Area = "doctor",
            Roles = ["doctor"]
        },
    ],
    Roles = ["doctor"]
},
                new ()
{
    Title = "Patients Management",
    Items = [
        new ()
        {
            Title = "Patients",
            Icon = "users",
            Action = "Index",
            Controller = "Patients",
            Area = "doctor",
            Roles = ["doctor"]
        },
        new ()
        {
            Title = "Add New Patient",
            Icon = "user-plus",
            Action = "Create",
            Controller = "Patients",
            Area = "Doctor",
            Roles = ["doctor"]
        }
    ],
    Roles = ["doctor"]
},
                new()
                {
                    Title = "System",
                    Items =
                    [
                        new()
                        {
                            Title = "System Settings",
                            Icon = "cogs",
                            Action = "index",
                            Controller = "system",
                            Area = "",
                            Roles = ["admin"]
                        },
                        new()
                        {
                            Title = "About System",
                            Icon = "info-circle",
                            Action = "systemsettings",
                            Controller = "system",
                            Area = "",
                            Roles = ["*"]
                        },
                         new()
                        {
                            Title = "System Logs",
                            Icon = "file-alt",
                            Action = "Logs",
                            Controller = "System",
                            Area = "",
                            Roles = ["admin"]
                        },
                        new()
                        {
                            Title = "Report",
                            Icon = "question",
                            Action = "",
                            Controller = "",
                            Area = "",
                            Roles = ["other"]
                        }
                    ],
                    Roles = ["*"]
                }
            };

        
            // Filter sidebar items by user roles
            var filteredElements = AllElements
                .Select(group => new SidebarItemsGroup()
                {
                    Title = group.Title,
                    Roles = group.Roles,
                    Items = group.Items
                        .Where(item => EnumerableHelper.HasCommonElement(item.Roles, Roles) || item.Roles.Contains("*"))
                        .ToList()
                })
                .Where(group => group.Items.Any())
                .ToList();

            return filteredElements;
        }
    }
}
