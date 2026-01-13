namespace IPTS.ViewModels
{
    public class CustomerProfileViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime RegisteredDate { get; set; }
    }
}
