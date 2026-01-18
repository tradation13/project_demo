namespace IPTS.Helpers
{
    public class HttpUser
    {
        public string userName { get; set; } = string.Empty;
        public string userId { get; set; } = string.Empty;
        public string ip { get; set; } = string.Empty;

        // 1. المدخل القديم (لا تلمسه) - عشان ما يخرب الـ Controllers
        public HttpUser(HttpContext httpContext)
        {
            Initialize(httpContext);
        }

        // 2. المدخل الجديد (إضافة) - عشان الـ UserService يقدر يشتغل تلقائياً
        public HttpUser(IHttpContextAccessor accessor)
        {
            Initialize(accessor.HttpContext);
        }

        // دالة موحدة لتعبئة البيانات عشان ما نكرر الكود
        private void Initialize(HttpContext? httpContext)
        {
            this.userName = httpContext?.User?.Identity?.Name ?? "Anonymous";
            this.ip = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "UnknownIP";
            // جرب جلب الـ ID بالطريقتين لضمان الدقة
            this.userId = httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                         ?? httpContext?.User?.FindFirst("sub")?.Value 
                         ?? "UnknownUser";
            
            Console.WriteLine($"HttpUser created: {userName}, {userId}, {ip}");
        }
    }
}