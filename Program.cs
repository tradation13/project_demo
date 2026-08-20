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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

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

            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                options.SignIn.RequireConfirmedEmail = true;
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/auth/accessdenied";
                options.LoginPath = "/auth/login";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    if (ApiRequestHelper.IsAjaxOrJsonRequest(context.Request))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (ApiRequestHelper.IsAjaxOrJsonRequest(context.Request))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });
            // builder.Services.AddControllersWithViews();

            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("AuthPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));

                // Chatbot persistence APIs (Guest + authenticated). Higher than AuthPolicy for normal chat turns.
                options.AddPolicy("ChatbotPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));

                options.OnRejected = async (context, cancellationToken) =>
                {
                    var path = context.HttpContext.Request.Path.Value ?? string.Empty;
                    if (path.StartsWith("/api/chatbot", StringComparison.OrdinalIgnoreCase))
                    {
                        LogHelper.LogWithContext(
                            $"Chatbot rate limit exceeded. path={path}",
                            context.HttpContext.User?.Identity?.Name ?? "Anonymous",
                            "Public",
                            "ChatbotRateLimit",
                            Serilog.Events.LogEventLevel.Warning);
                    }

                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    if (!context.HttpContext.Response.HasStarted)
                    {
                        context.HttpContext.Response.ContentType = "application/json";
                        await context.HttpContext.Response.WriteAsync(
                            "{\"success\":false,\"message\":\"Too many requests.\"}",
                            cancellationToken);
                    }
                };
            });
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
            builder.Services.AddScoped<MedicalCaseTestPhotoService>();
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
            builder.Services.AddScoped<BlogPostService>();
            builder.Services.AddScoped<AuditService>();
            builder.Services.AddScoped<ChatbotService>();
            
            // Register FileService for prescription file handling
            builder.Services.AddScoped<IFileService, FileService>();
            
            builder.Services.AddOutputCache();
            builder.Services.AddMemoryCache();
            var app = builder.Build();

// 1. السماح بالوصول للملفات الثابتة العادية في wwwroot
app.UseStaticFiles(); 

// 2. تعريف مسار مجلد الصور (خارج wwwroot)
var internalStoragePath = Path.Combine(builder.Environment.ContentRootPath, "InternalStorage");

// تأكد أن المجلد موجود عشان ما يرمي Exception ويقفل الموقع
if (!Directory.Exists(internalStoragePath))
{
    Directory.CreateDirectory(internalStoragePath);
}

// 3. منح تصريح مرور لمجلد InternalStorage
// قبل الحماية
// app.UseStaticFiles(new StaticFileOptions
// {
//     FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(internalStoragePath),
//     RequestPath = "/InternalStorage"
// });

// بعد الحماية
var publicStorageFolders = new[] { "BlogsImages", "DoctorPhotos" };

foreach (var folderName in publicStorageFolders)
{
    var folderPath = Path.Combine(internalStoragePath, folderName);

    if(!Directory.Exists(folderPath))
    Directory.CreateDirectory(folderPath);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(folderPath),
        RequestPath = $"/InternalStorage/{folderName}"
    });     
}

            // 4. إعداد اللغات (Middleware)
var supportedCultures = new[] { "en-US", "de-DE"};
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "/";
    var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
    
    if (segments.Length > 0)
    {
        var lastSegment = segments[^1];
        string? culture = null;

        if (string.Equals(lastSegment, "de", StringComparison.OrdinalIgnoreCase))
        {
            culture = "de-DE";
        }
        else if (string.Equals(lastSegment, "en", StringComparison.OrdinalIgnoreCase))
        {
            culture = "en-US";
        }

        if (culture != null)
        {
            // حفظ الكوكي فوراً في الـ Response
            context.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Path = "/" });

            // **الحل السحري للطلب الأول**: نقوم بحقن القيمة مباشرة في الـ Request من خلال الـ Query أو الكوكيز للطلب الحالي
            context.Request.Headers["Cookie"] = $"{CookieRequestCultureProvider.DefaultCookieName}={CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture))}";

            // تنظيف الرابط داخلياً
            context.Request.Path = segments.Length == 1
                ? "/"
                : new PathString($"/{string.Join('/', segments[..^1])}");
        }
    }

    await next();
});

app.UseRequestLocalization(localizationOptions);

    
            app.UseHttpsRedirection();
            //app.UseRouting();

            app.UseRouting();
            app.UseRateLimiter();

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
