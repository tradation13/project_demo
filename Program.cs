using IPTS.Resources; // استيراد مسار الخدمة الجديد
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using FluentValidation;
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
using IPTS.Models.Sidebar;
using FluentValidation.AspNetCore;

namespace IPTS
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Batteries.Init(); 

            var builder = WebApplication.CreateBuilder(args);


// 1. إضافة الـ Localization وتحديد مجلد الموارد **أولاً**
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// 2. تسجيل LocService كـ Singleton **ثانياً** (يحتاجه RegisterValidator)
builder.Services.AddSingleton<LocService>();

// 3. تسجيل كل الـ Validators الموجودة في المشروع **بعد LocService**
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// 4. تفعيل الفحص التلقائي (Auto Validation) لكي لا تضطر لكتابة كود فحص في كل Controller
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddMvc(opt =>
{
	opt.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// 5. إعداد MVC مع دعم الـ SharedResource للـ DataAnnotations
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
        {
           // قمنا بتغيير SharedResource إلى AppResource هنا فقط
            var assemblyName = new System.Reflection.AssemblyName(typeof(SystemResource).Assembly.FullName!);
            return factory.Create("SystemResource", assemblyName.Name!);
        };
    });

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
            // builder.Services.AddControllersWithViews();

            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();
            builder.Services.AddAutoMapper(typeof(MappingProfile));

            // ✅ تسجيل IdentityErrorTranslator لترجمة أخطاء Identity
            builder.Services.AddScoped<IdentityErrorTranslator>();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<HttpUser>();

            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<UserTypeService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<PdfPrintService>();
            builder.Services.AddScoped<TestGroupService>();
            builder.Services.AddScoped<MedicalCaseService>();
            builder.Services.AddScoped<MedicalCaseTestService>();
            // builder.Services.AddScoped<MedicalReportService>();
            builder.Services.AddHttpClient<MedicalReportService>(); // تسجيل MedicalReportService مع دعم HttpClient
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
            
            // Register FileService for prescription file handling
            builder.Services.AddScoped<IFileService, FileService>();
            
            builder.Services.AddOutputCache();
            builder.Services.AddMemoryCache();
            var app = builder.Build();

              app.UseStaticFiles();

            // 4. إعداد اللغات (Middleware)
var supportedCultures = new[] { "en-US", "de-DE"};
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[1])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

    
            app.UseHttpsRedirection();
            //app.UseRouting();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllerRoute(
                 name: "areas",
                 pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
             );

          

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
