namespace IPTS.ViewModels
{
    public class GuestAuthSuccessDto
    {
        public bool IsEmailConfirmed { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
