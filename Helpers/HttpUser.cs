namespace IPTS.Helpers
{
    public class HttpUser
    {
        public string userName { get; set; } = string.Empty;
        public string userId { get; set; } = string.Empty;
        public string ip { get; set; } = string.Empty;
        
        public HttpUser(HttpContext httpContext)
        {
             this.userName = httpContext?.User?.Identity?.Name ?? "Anonymous";
             this.ip = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "UnknownIP";
             this.userId = httpContext?.User?.FindFirst("sub")?.Value ?? "UnknownUser";
             Console.WriteLine($"HttpUser created: {userName}, {userId}, {ip}");
        }
    }
}