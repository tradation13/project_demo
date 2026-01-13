using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

namespace IPTS.Data.Bootstrap
{
    public static class ServiceProviderExtensions
    {
        public static async Task EnsureInfrastructureAsync(this IServiceProvider services)
        {
            try
            {
                Console.WriteLine("--- Botsrtap Start --");

                using var scope = services.CreateScope();
                var sp = scope.ServiceProvider;


                var db = sp.GetRequiredService<ApplicationDbContext>();
                await db.Database.MigrateAsync();

                Console.WriteLine("SeedIdentity Start ...");

                await SeedIdentityAsync(sp);

                Console.WriteLine("SeedIdentity End ...");
                Console.WriteLine("--- Botsrtap End --");


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private static async Task SeedIdentityAsync(IServiceProvider sp)
        {
            var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = sp.GetRequiredService<UserManager<AppUser>>();
            var userTypeServ = sp.GetRequiredService<UserTypeService>();
            var cfg = sp.GetRequiredService<IConfiguration>();

            // Check Rules Exists And Add Them
            string[] roles = { "patient", "admin", "doctor" };
            foreach (var r in roles)
                if (!await roleMgr.RoleExistsAsync(r))
                {
                    await roleMgr.CreateAsync(new IdentityRole(r));
                    Console.WriteLine($"{r} was Created...");
                }
                  
            Console.WriteLine("Rules Checked was Finised ...");

            // Check UserTypes Exists And Add Them
            UserType[] UserTypes = {
                new() { Name = "patient", DefaultRoleId = (await roleMgr.FindByNameAsync("patient")).Id,HasDashboard=false, DefaultArea="patient", DefaultController="Panel", Registerable=true},
                new() { Name = "Admin",  DefaultRoleId = (await roleMgr.FindByNameAsync("admin")).Id, RequireAdminApproval = true, DefaultArea="admin", DefaultController="Dashboard", HasDashboard=true, Registerable=false},
                new() { Name = "doctor",  DefaultRoleId = (await roleMgr.FindByNameAsync("doctor")).Id, RequireAdminApproval = true, DefaultArea="doctor", DefaultController="Dashboard", HasDashboard=true, Registerable=true},
            };
            foreach (var userType in UserTypes)
            {
                if (!await userTypeServ.IsExistAsync(ut => ut.Name == userType.Name))
                {
                    await userTypeServ.AddAsync(userType);
                    Console.WriteLine($"{userType.Name} was Created...");
                }
            }
            Console.WriteLine("Usertypes Checked was Finised ...");

            var email = cfg["Bootstrap:SuperAdminEmail"];
            var pass = cfg["Bootstrap:SuperAdminPassword"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
                throw new InvalidOperationException("Bootstrap credentials are missing.");

            var super = await userMgr.FindByEmailAsync(email);
            if (super is null)
            {
                var u = new AppUser { UserName = email, Email = email, EmailConfirmed=true,Status=EnUserStatus.Active, FirstName="Super", LastName="Admin", UserTypeId=(await userTypeServ.GetAllAsync(q=>q.Where(u => u.Name == "Admin")))[0].Id };
                var res = await userMgr.CreateAsync(u, pass);
                if (!res.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", res.Errors.Select(e => e.Description)));

                await userMgr.AddToRoleAsync(u, "Admin");
           
            }
        }
    }
}
