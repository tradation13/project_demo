namespace IPTS.ViewModels
{
    public class LoginViewModel
    {
        public string UsernameOrEmail { get; set; } = string.Empty;
        public string Password{ get; set;} = string.Empty;
        public bool RememberMe { get; set; } = false;
        public string? ReturnUrl { get; set; }
    }
}
