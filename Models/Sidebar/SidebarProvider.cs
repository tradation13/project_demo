using IPTS.Helpers;
using IPTS.Resources;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Security.Claims;

namespace IPTS.Models.Sidebar
{
    public static class SidebarProvider
    {
       public static List<SidebarItemsGroup> GetSidebarItems(ClaimsPrincipal User,LocService _loc)
        {    // Extract user roles from claims
            var Roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(cr => cr.Value);

            var AllElements = new List<SidebarItemsGroup>()
            {
                new()
                {
                    Title = _loc["General"],
                    Items =
                    [
                        new()
                        {
                            Title = _loc["Dashboard"], // later will make it dynamic - each account to its own dashboard -
                            Icon = "dashboard",
                            Action = "Index",
                            Controller = "Dashboard",
                            Area = Roles.First(), // Get it from Database based on account 
                            Roles = ["*"]
                        },
                        new()
                        {
                            Title = _loc["Shared_Profile"],
                            Icon = "user",
                            Action = "Index",
                            Controller = "Profile",
                            Area = "",
                            Roles = ["*"]
                        },
                         new()
                        {
                            Title = _loc["Shared_ResetPassword"],
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
                    Title = _loc["UserManagement"],
                    Items =
                    [
                        new()
                        {
                            Title = _loc["Users"],
                            Icon = "users",
                            Action = "Index",
                            Controller = "Users",
                            Area = "admin",
                            Roles = ["admin"]
                        },
                        new()
                        {
                            Title = _loc["Roles"],
                            Icon = "user-shield",
                            Action = "Index",
                            Controller = "Roles",
                            Area = "admin",
                            Roles = ["admin"]
                        },
                        new()
                        {
                            Title = _loc["Permissions"],
                            Icon = "key",
                            Action = "Index",
                            Controller = "Permissions",
                            Area = "admin",
                            Roles = ["admin"]
                        },
                        new()
                        {
                            Title = _loc["UserTypes"],
                            Icon = "id-badge",
                            Action = "Index",
                            Controller = "UsersTypes",
                            Area = "admin",
                            Roles = ["admin"]
                        },
                        new()
                        {
                            Title = _loc["Policies"],
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
                    Title = _loc["TestsManagement"],
                    Items = [
                        new ()
                        {
                            Title = _loc["TestGroups"],
                            Icon = "layer-group",
                            Action = "Index",
                            Controller = "TestGroups",
                            Area = "admin",
                            Roles = ["admin"]
                        },
                        new ()
                        {
                            Title = _loc["Tests"],
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
    Title = _loc["AppointmentManagement"],
    Items = [
        new ()
        {
            Title = _loc["Appointments"],
            Icon = "calendar-check",
            Action = "Index",
            Controller = "Appointments",
            Area = "doctor",
            Roles = ["doctor"]
        },
        new ()
        {
            Title = _loc["AppointmentRequests"],
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
    Title = _loc["PatientsManagement"],
    Items = [
        new ()
        {
            Title = _loc["Patients"],
            Icon = "users",
            Action = "Index",
            Controller = "Patients",
            Area = "doctor",
            Roles = ["doctor"]
        },
        new ()
        {
            Title = _loc["AddNewPatient"],
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
                    Title = _loc["System"],
                    Items =
                    [
                        new()
                        {
                            Title = _loc["SystemSettings"],
                            Icon = "cogs",
                            Action = "index",
                            Controller = "system",
                            Area = "",
                            Roles = ["admin"]
                        },
                        new()
                        {
                            Title = _loc["AboutSystem"],
                            Icon = "info-circle",
                            Action = "systemsettings",
                            Controller = "system",
                            Area = "",
                            Roles = ["*"]
                        },
                         new()
                        {
                            Title = _loc["SystemLogs"],
                            Icon = "file-alt",
                            Action = "Logs",
                            Controller = "System",
                            Area = "",
                            Roles = ["admin"]
                        },
                        new()
                        {
                            Title = _loc["Report"],
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
