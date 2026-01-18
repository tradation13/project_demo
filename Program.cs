using IPTS.Data;
using IPTS.Data.Bootstrap;
using IPTS.Helpers;
using IPTS.Mapper;
using IPTS.Models.Entites;
using IPTS.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog;
using SQLitePCL;
using System;
using System.Data;
using System.Diagnostics;

namespace IPTS
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Batteries.Init(); 

            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, config) =>
            {
                config.ReadFrom.Configuration(context.Configuration);
                config.Enrich.FromLogContext(); 
            });

            builder.WebHost.UseWebRoot(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            builder.Services.AddIdentity<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/auth/accessdenied";
                options.LoginPath = "/auth/login";
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;

            });
            builder.Services.AddControllersWithViews();

            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();
            builder.Services.AddAutoMapper(typeof(MappingProfile));

          
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<HttpUser>();

            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<UserTypeService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<PdfPrintService>();
            builder.Services.AddScoped<TestGroupService>();
            builder.Services.AddScoped<MedicalCaseService>();
            builder.Services.AddScoped<MedicalCaseTestService>();
            builder.Services.AddScoped<TestService>();
            builder.Services.AddScoped<PatientService>();
            builder.Services.AddScoped<IDbConnection>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                return new NpgsqlConnection(connectionString); // Ensure you have the Npgsql package installed.
            });
            builder.Services.AddScoped<SystemService>();
            builder.Services.AddScoped<SpecialtyService>();
            builder.Services.AddScoped<AppointmentService>();
            builder.Services.AddOutputCache();
            builder.Services.AddMemoryCache();
            var app = builder.Build();

    
            app.UseHttpsRedirection();
            //app.UseRouting();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllerRoute(
                 name: "areas",
                 pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
             );

            app.UseStaticFiles();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}"
            );

            
            //app.MapRazorPages();

            //await app.Services.EnsureInfrastructureAsync();

            //app.Use(async (context, next) =>
            //{
            //    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            //    var stopwatch = new Stopwatch();

            //    stopwatch.Start();

            //    // Request - Start

            //    logger.LogCritical($"Request Path: {context.Request.Path}");

            //    await next();
            //    // Response - End 
            //    stopwatch.Stop();

            //    logger.LogCritical($"Loading Time: {stopwatch.ElapsedMilliseconds}ms");
            //    stopwatch.Reset();
            //});

            app.Run();
        }
    }
}
