namespace Patient_Information_System_CS.Models
{
    public class AccountProfileDetails
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string GivenName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Suffix { get; set; }
    }

    public sealed class AccountProfileUpdate : AccountProfileDetails
    {
        public string? NewPassword { get; set; }
    }
}
