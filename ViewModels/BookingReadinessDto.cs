namespace IPTS.ViewModels
{
    public class BookingReadinessDto
    {
        public bool IsAuthenticated { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public string Role { get; set; } = string.Empty;
        public List<string> Missing { get; set; } = new();
        public bool CanBook { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
